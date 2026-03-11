using DiplomApp.Models;
using DiplomApp.Helpers;

namespace DiplomApp.Services
{
    public class InMemoryReservationService : IReservationService
    {
        private readonly List<Reservation> _reservations = new();

        public Task<IReadOnlyList<Reservation>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Reservation> snapshot = _reservations
                .OrderBy(r => r.ReservationDateTime)
                .ToList();
            return Task.FromResult(snapshot);
        }

        public Task<IReadOnlyList<Reservation>> GetReservationsByUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Reservation> userReservations = _reservations
                .Where(r => r.UserId == userId)
                .OrderBy(r => r.ReservationDateTime)
                .ToList();
            return Task.FromResult(userReservations);
        }

        public Task<IReadOnlyList<Reservation>> GetReservationsBySpecialtyAsync(MedicalSpecialty specialty, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Reservation> specialtyReservations = _reservations
                .Where(r => r.MedicalSpecialty == specialty)
                .OrderBy(r => r.ReservationDateTime)
                .ToList();
            return Task.FromResult(specialtyReservations);
        }

        public async Task<Reservation> CreateAsync(Reservation reservation, CancellationToken cancellationToken = default)
        {
            // Validate time slot (30-minute slots)
            if (!TimeSlotHelper.IsValid30MinuteTimeSlot(reservation.ReservationDateTime))
            {
                throw new InvalidOperationException("Invalid time slot. Please select a valid 30-minute time slot between 8:00 AM and 7:30 PM.");
            }

            var now = DateTime.Now;
            var reservationDate = reservation.ReservationDateTime.Date;
            
            // Check user's active reservations (future reservations only)
            var activeReservations = _reservations
                .Where(r => r.UserId == reservation.UserId && r.ReservationDateTime >= now)
                .ToList();
            
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

            // Check if slot is available for the selected specialty
            if (!await IsTimeSlotAvailableAsync(reservation.ReservationDateTime, reservation.MedicalSpecialty, cancellationToken))
            {
                throw new InvalidOperationException("This time slot is not available for the selected specialty.");
            }

            reservation.Id = reservation.Id == Guid.Empty ? Guid.NewGuid() : reservation.Id;
            _reservations.Add(reservation);
            return reservation;
        }

        public Task<bool> DeleteAsync(Guid reservationId, string? userId = null, CancellationToken cancellationToken = default)
        {
            // In-memory implementation doesn't check user roles - always allow deletion
            var removed = _reservations.RemoveAll(r => r.Id == reservationId) > 0;
            return Task.FromResult(removed);
        }

        public Task<List<DateTime>> GetAvailableTimeSlotsAsync(DateTime date, MedicalSpecialty? specialty = null, CancellationToken cancellationToken = default)
        {
            // Get all 30-minute time slots for the date
            var allSlots = TimeSlotHelper.GetTimeSlots30MinutesForDate(date);
            
            // Filter out past slots if it's today
            var now = DateTime.Now;
            if (date.Date == now.Date)
            {
                allSlots = allSlots.Where(s => s > now).ToList();
            }
            
            // Filter out booked slots
            var bookedSlotsQuery = _reservations.Where(r => r.ReservationDateTime.Date == date.Date);
            if (specialty.HasValue)
            {
                bookedSlotsQuery = bookedSlotsQuery.Where(r => r.MedicalSpecialty == specialty.Value);
            }
            
            var bookedSlots = bookedSlotsQuery
                .Select(r => r.ReservationDateTime)
                .ToHashSet();
            
            var availableSlots = allSlots
                .Where(s => !bookedSlots.Contains(s))
                .ToList();
            
            return Task.FromResult(availableSlots);
        }

        public Task<bool> IsTimeSlotAvailableAsync(DateTime slot, MedicalSpecialty? specialty = null, CancellationToken cancellationToken = default)
        {
            // Check if slot is in the past
            if (slot <= DateTime.Now)
                return Task.FromResult(false);
            
            // Validate it's a 30-minute slot
            if (!TimeSlotHelper.IsValid30MinuteTimeSlot(slot))
                return Task.FromResult(false);
            
            // Check if slot is already booked
            var bookedQuery = _reservations.Where(r => r.ReservationDateTime == slot);
            if (specialty.HasValue)
            {
                bookedQuery = bookedQuery.Where(r => r.MedicalSpecialty == specialty.Value);
            }
            
            bool isBooked = bookedQuery.Any();
            return Task.FromResult(!isBooked);
        }
    }
}




