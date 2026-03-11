using DiplomApp.Models;
using DiplomApp.Helpers;
using DiplomApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using Microsoft.AspNetCore.Identity;

namespace DiplomApp.Services
{
    public class DatabaseReservationService : IReservationService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DatabaseReservationService> _logger;
        private readonly IDoctorAvailabilityService _doctorAvailabilityService;

        public DatabaseReservationService(
            IServiceScopeFactory scopeFactory,
            ILogger<DatabaseReservationService> logger,
            IDoctorAvailabilityService doctorAvailabilityService)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _doctorAvailabilityService = doctorAvailabilityService;
        }

        public async Task<IReadOnlyList<Reservation>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var reservations = await context.Reservations
                .OrderBy(r => r.ReservationDateTime)
                .ToListAsync(cancellationToken);
            
            return reservations;
        }

        public async Task<IReadOnlyList<Reservation>> GetReservationsByUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var reservations = await context.Reservations
                .Where(r => r.UserId == userId)
                .OrderBy(r => r.ReservationDateTime)
                .ToListAsync(cancellationToken);
            
            return reservations;
        }

        public async Task<IReadOnlyList<Reservation>> GetReservationsBySpecialtyAsync(MedicalSpecialty specialty, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var reservations = await context.Reservations
                .Where(r => r.MedicalSpecialty == specialty)
                .OrderBy(r => r.ReservationDateTime)
                .ToListAsync(cancellationToken);
            
            return reservations;
        }

        public async Task<Reservation> CreateAsync(Reservation reservation, CancellationToken cancellationToken = default)
        {
            // Validate time slot (30-minute slots)
            if (!TimeSlotHelper.IsValid30MinuteTimeSlot(reservation.ReservationDateTime))
            {
                throw new InvalidOperationException("Invalid time slot. Please select a valid 30-minute time slot between 8:00 AM and 7:30 PM.");
            }

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Use transaction with Serializable isolation level to prevent race conditions
            using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            
            try
            {
                var now = DateTime.Now;
                var reservationDate = reservation.ReservationDateTime.Date;
                
                // Business rule: Minimum 30 minutes advance booking
                var minimumBookingTime = now.AddMinutes(30);
                if (reservation.ReservationDateTime <= minimumBookingTime)
                {
                    throw new InvalidOperationException($"Nie można zarezerwować wizyty mniej niż 30 minut przed terminem. Najbliższy możliwy termin to {minimumBookingTime:dd.MM.yyyy HH:mm}.");
                }
                
                // Check user's active reservations (future reservations only)
                var activeReservations = await context.Reservations
                    .Where(r => r.UserId == reservation.UserId && r.ReservationDateTime >= now)
                    .ToListAsync(cancellationToken);
                
                // Check if user already has 3 or more active reservations
                if (activeReservations.Count >= 3)
                {
                    throw new InvalidOperationException("You cannot have more than 3 active reservations. Please cancel an existing reservation first.");
                }
                
                // Check if user already has a reservation on the same day
                var hasReservationOnSameDay = activeReservations
                    .Any(r => r.ReservationDateTime.Date == reservationDate);
                
                if (hasReservationOnSameDay)
                {
                    throw new InvalidOperationException("You already have a reservation on this day. You can only have one reservation per day.");
                }
                
                // Re-check if slot is available for the selected specialty (inside transaction to prevent race condition)
                var slotExists = await context.Reservations
                    .AnyAsync(r => r.ReservationDateTime == reservation.ReservationDateTime && 
                                  r.MedicalSpecialty == reservation.MedicalSpecialty, cancellationToken);
                
                if (slotExists)
                {
                    throw new InvalidOperationException("This time slot is not available for the selected specialty. It may have just been booked by another user.");
                }
                
                // Check if any doctor with this specialty exists
                var doctorsWithSpecialty = await context.Users
                    .Where(u => u.MedicalSpecialty == reservation.MedicalSpecialty)
                    .Select(u => u.Id)
                    .AnyAsync(cancellationToken);
                
                if (!doctorsWithSpecialty)
                {
                    throw new InvalidOperationException("No doctors available for the selected specialty.");
                }
                
                // Note: Doctor availability check is done outside transaction to avoid scope issues
                // The unique constraint will prevent double booking even if availability check passes

                // Ensure we have a valid ID
                if (reservation.Id == Guid.Empty)
                {
                    reservation.Id = Guid.NewGuid();
                }

                context.Reservations.Add(reservation);
                await context.SaveChangesAsync(cancellationToken);
                
                // Commit transaction
                await transaction.CommitAsync(cancellationToken);
                
                _logger.LogInformation("Reservation {ReservationId} created successfully for user {UserId}", 
                    reservation.Id, reservation.UserId);
                
                // Send confirmation email (fire and forget - don't let email failures break the reservation)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var emailScope = _scopeFactory.CreateScope();
                        var emailService = emailScope.ServiceProvider.GetRequiredService<IEmailService>();
                        await emailService.SendReservationConfirmationAsync(reservation, CancellationToken.None);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send confirmation email for reservation {ReservationId}", reservation.Id);
                    }
                }, CancellationToken.None);
                
                return reservation;
            }
            catch (DbUpdateException dbEx) when (dbEx.InnerException?.Message?.Contains("UNIQUE constraint") == true)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogWarning("Race condition detected: Slot {DateTime} for {Specialty} was already booked", 
                    reservation.ReservationDateTime, reservation.MedicalSpecialty);
                throw new InvalidOperationException("This time slot was just booked by another user. Please select a different time.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error creating reservation for user {UserId}", reservation.UserId);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid reservationId, string? userId = null, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            try
            {
                var reservation = await context.Reservations
                    .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken);
                
                if (reservation == null)
                {
                    _logger.LogWarning("Reservation {ReservationId} not found for deletion", reservationId);
                    return false;
                }

                // Business rule: Minimum 12 hours advance cancellation - tylko dla Customer
                // Admin i Employee mogą anulować w dowolnym momencie
                bool shouldCheckTimeLimit = true;
                
                if (!string.IsNullOrEmpty(userId))
                {
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                    var user = await userManager.FindByIdAsync(userId);
                    
                    if (user != null)
                    {
                        var roles = await userManager.GetRolesAsync(user);
                        // Jeśli użytkownik jest Admin lub Employee, pomijamy limit czasowy
                        if (roles.Contains("Admin") || roles.Contains("Employee"))
                        {
                            shouldCheckTimeLimit = false;
                        }
                    }
                }

                if (shouldCheckTimeLimit)
                {
                    var timeUntilAppointment = reservation.ReservationDateTime - DateTime.Now;
                    if (timeUntilAppointment.TotalHours < 12)
                    {
                        throw new InvalidOperationException($"Nie można anulować wizyty mniej niż 12 godzin przed terminem. Termin wizyty: {reservation.ReservationDateTime:dd.MM.yyyy HH:mm}. Obecnie: {DateTime.Now:dd.MM.yyyy HH:mm}.");
                    }
                }

                context.Reservations.Remove(reservation);
                await context.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Reservation {ReservationId} deleted successfully by user {UserId}", reservationId, userId ?? "unknown");
                return true;
            }
            catch (InvalidOperationException)
            {
                // Re-throw business rule violations
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting reservation {ReservationId}", reservationId);
                throw;
            }
        }

        public async Task<List<DateTime>> GetAvailableTimeSlotsAsync(DateTime date, MedicalSpecialty? specialty = null, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Get all 30-minute time slots for the date
            var allSlots = TimeSlotHelper.GetTimeSlots30MinutesForDate(date);
            
            // Business rule: Filter out slots that are less than 30 minutes in the future
            var now = DateTime.Now;
            var minimumBookingTime = now.AddMinutes(30);
            allSlots = allSlots.Where(s => s > minimumBookingTime).ToList();
            
            // If specialty is specified, get available slots from doctors with that specialty
            if (specialty.HasValue)
            {
                // Get all doctors with this specialty
                var doctorsWithSpecialty = await context.Users
                    .Where(u => u.MedicalSpecialty == specialty.Value)
                    .Select(u => u.Id)
                    .ToListAsync(cancellationToken);
                
                if (!doctorsWithSpecialty.Any())
                {
                    // No doctors with this specialty, return empty list
                    return new List<DateTime>();
                }
                
                // Get all available slots from all doctors with this specialty
                var allAvailableSlots = new HashSet<DateTime>();
                foreach (var doctorId in doctorsWithSpecialty)
                {
                    var doctorSlots = await _doctorAvailabilityService.GetAvailableSlotsForDateAsync(doctorId, date, cancellationToken);
                    foreach (var slot in doctorSlots)
                    {
                        allAvailableSlots.Add(slot);
                    }
                }
                
                // Filter to only slots that are in allSlots (30-minute intervals)
                var specialtyAvailableSlots = allSlots
                    .Where(s => allAvailableSlots.Contains(s))
                    .ToList();
                
                // Get booked slots from database for this specialty
                var bookedSlots = await context.Reservations
                    .Where(r => r.ReservationDateTime.Date == date.Date && r.MedicalSpecialty == specialty.Value)
                    .Select(r => r.ReservationDateTime)
                    .ToListAsync(cancellationToken);
                
                var bookedSlotsSet = bookedSlots.ToHashSet();
                
                // Filter out booked slots
                return specialtyAvailableSlots
                    .Where(s => !bookedSlotsSet.Contains(s))
                    .OrderBy(s => s)
                    .ToList();
            }
            else
            {
                // No specialty specified - return all slots (legacy behavior, but filter booked ones)
                var bookedSlots = await context.Reservations
                    .Where(r => r.ReservationDateTime.Date == date.Date)
                    .Select(r => r.ReservationDateTime)
                    .ToListAsync(cancellationToken);
                
                var bookedSlotsSet = bookedSlots.ToHashSet();
                
                return allSlots
                    .Where(s => !bookedSlotsSet.Contains(s))
                    .OrderBy(s => s)
                    .ToList();
            }
        }

        public async Task<bool> IsTimeSlotAvailableAsync(DateTime slot, MedicalSpecialty? specialty = null, CancellationToken cancellationToken = default)
        {
            // Business rule: Minimum 30 minutes advance booking
            var minimumBookingTime = DateTime.Now.AddMinutes(30);
            if (slot <= minimumBookingTime)
                return false;
            
            // Validate it's a 30-minute slot
            if (!TimeSlotHelper.IsValid30MinuteTimeSlot(slot))
                return false;
            
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Check if slot is already booked in database
            var query = context.Reservations.Where(r => r.ReservationDateTime == slot);
            if (specialty.HasValue)
            {
                query = query.Where(r => r.MedicalSpecialty == specialty.Value);
            }
            bool isBooked = await query.AnyAsync(cancellationToken);
            
            if (isBooked)
                return false;
            
            // If specialty is specified, check if any doctor with that specialty is available
            if (specialty.HasValue)
            {
                var doctorsWithSpecialty = await context.Users
                    .Where(u => u.MedicalSpecialty == specialty.Value)
                    .Select(u => u.Id)
                    .ToListAsync(cancellationToken);
                
                if (!doctorsWithSpecialty.Any())
                    return false;
                
                // Check if at least one doctor with this specialty has this slot available
                foreach (var doctorId in doctorsWithSpecialty)
                {
                    if (await _doctorAvailabilityService.IsSlotAvailableAsync(doctorId, slot, cancellationToken))
                    {
                        return true; // At least one doctor is available
                    }
                }
                
                return false; // No doctors available for this slot
            }
            
            return true;
        }
    }
}

