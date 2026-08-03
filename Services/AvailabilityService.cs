using DiplomApp.Models;
using DiplomApp.Data;
using DiplomApp.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DiplomApp.Services
{
    public class AvailabilityService : IAvailabilityService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AvailabilityService> _logger;

        public AvailabilityService(
            ApplicationDbContext context,
            ILogger<AvailabilityService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DoctorAvailability?> GetAvailabilityAsync(string doctorId, DateTime date, CancellationToken cancellationToken = default)
        {

            return await _context.DoctorAvailabilities
                .FirstOrDefaultAsync(da => da.DoctorId == doctorId && da.Date.Date == date.Date, cancellationToken);
        }

        public async Task<List<DoctorAvailability>> GetAvailabilitiesAsync(string doctorId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        {

            return await _context.DoctorAvailabilities
                .Where(da => da.DoctorId == doctorId && da.Date.Date >= fromDate.Date && da.Date.Date <= toDate.Date)
                .OrderBy(da => da.Date)
                .ToListAsync(cancellationToken);
        }

        public async Task<DoctorAvailability> SaveAvailabilityAsync(string doctorId, DateTime date, List<int> availableSlots, CancellationToken cancellationToken = default)
        {

            // Validate slots are valid 30-minute intervals
            var validSlots = TimeSlotHelper.GetAll30MinuteSlotMinutes();
            var invalidSlots = availableSlots.Where(s => !validSlots.Contains(s)).ToList();
            if (invalidSlots.Any())
            {
                throw new ArgumentException($"Nieprawidłowe terminy: {string.Join(", ", invalidSlots)}");
            }

            var existing = await _context.DoctorAvailabilities
                .FirstOrDefaultAsync(da => da.DoctorId == doctorId && da.Date.Date == date.Date, cancellationToken);

            var slotsJson = JsonSerializer.Serialize(availableSlots.OrderBy(s => s).ToList());

            if (existing != null)
            {
                existing.AvailableSlots = slotsJson;
                existing.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Updated availability for doctor {DoctorId} on {Date}", doctorId, date.Date);
                return existing;
            }
            else
            {
                var availability = new DoctorAvailability
                {
                    DoctorId = doctorId,
                    Date = date.Date,
                    AvailableSlots = slotsJson,
                    CreatedAt = DateTime.UtcNow
                };

                _context.DoctorAvailabilities.Add(availability);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Created availability for doctor {DoctorId} on {Date}", doctorId, date.Date);
                return availability;
            }
        }

        public async Task<bool> DeleteAvailabilityAsync(string doctorId, DateTime date, CancellationToken cancellationToken = default)
        {

            var availability = await _context.DoctorAvailabilities
                .FirstOrDefaultAsync(da => da.DoctorId == doctorId && da.Date.Date == date.Date, cancellationToken);

            if (availability == null)
                return false;

            _context.DoctorAvailabilities.Remove(availability);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Usunięto dostępność dla lekarza {DoctorId} na dzień {Date}", doctorId, date.Date);
            return true;
        }

        public async Task<bool> IsSlotAvailableAsync(string doctorId, DateTime slot, CancellationToken cancellationToken = default)
        {
            if (!TimeSlotHelper.IsValid30MinuteTimeSlot(slot))
                return false;

            var availability = await GetAvailabilityAsync(doctorId, slot.Date, cancellationToken);
            if (availability == null)
                return false;

            var availableSlots = JsonSerializer.Deserialize<List<int>>(availability.AvailableSlots) ?? new List<int>();
            var slotMinutes = TimeSlotHelper.DateTimeToMinutes(slot);

            return availableSlots.Contains(slotMinutes);
        }

        public async Task<List<DateTime>> GetAvailableSlotsForDateAsync(string doctorId, DateTime date, CancellationToken cancellationToken = default)
        {
            var availability = await GetAvailabilityAsync(doctorId, date, cancellationToken);
            if (availability == null)
                return new List<DateTime>();

            var availableSlots = JsonSerializer.Deserialize<List<int>>(availability.AvailableSlots) ?? new List<int>();
            
            return availableSlots
                .Select(minutes => TimeSlotHelper.MinutesToDateTime(date.Date, minutes))
                .Where(slot => slot >= DateTime.Now) // Filter out past slots
                .OrderBy(slot => slot)
                .ToList();
        }
    }
}

