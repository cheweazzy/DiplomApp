using DiplomApp.Models;
using DiplomApp.Data;
using DiplomApp.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomApp.Services
{
    public class DoctorAvailabilityService : IDoctorAvailabilityService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DoctorAvailabilityService> _logger;

        public DoctorAvailabilityService(
            IServiceScopeFactory scopeFactory,
            ILogger<DoctorAvailabilityService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<DoctorAvailability?> GetAvailabilityAsync(string doctorId, DateTime date, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await context.DoctorAvailabilities
                .FirstOrDefaultAsync(da => da.DoctorId == doctorId && da.Date.Date == date.Date, cancellationToken);
        }

        public async Task<List<DoctorAvailability>> GetAvailabilitiesAsync(string doctorId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await context.DoctorAvailabilities
                .Where(da => da.DoctorId == doctorId && da.Date.Date >= fromDate.Date && da.Date.Date <= toDate.Date)
                .OrderBy(da => da.Date)
                .ToListAsync(cancellationToken);
        }

        public async Task<DoctorAvailability> SaveAvailabilityAsync(string doctorId, DateTime date, List<int> availableSlots, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Validate slots are valid 30-minute intervals
            var validSlots = TimeSlotHelper.GetAll30MinuteSlotMinutes();
            var invalidSlots = availableSlots.Where(s => !validSlots.Contains(s)).ToList();
            if (invalidSlots.Any())
            {
                throw new ArgumentException($"Invalid time slots: {string.Join(", ", invalidSlots)}");
            }

            var existing = await context.DoctorAvailabilities
                .FirstOrDefaultAsync(da => da.DoctorId == doctorId && da.Date.Date == date.Date, cancellationToken);

            var slotsJson = JsonSerializer.Serialize(availableSlots.OrderBy(s => s).ToList());

            if (existing != null)
            {
                existing.AvailableSlots = slotsJson;
                existing.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
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

                context.DoctorAvailabilities.Add(availability);
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Created availability for doctor {DoctorId} on {Date}", doctorId, date.Date);
                return availability;
            }
        }

        public async Task<bool> DeleteAvailabilityAsync(string doctorId, DateTime date, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var availability = await context.DoctorAvailabilities
                .FirstOrDefaultAsync(da => da.DoctorId == doctorId && da.Date.Date == date.Date, cancellationToken);

            if (availability == null)
                return false;

            context.DoctorAvailabilities.Remove(availability);
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted availability for doctor {DoctorId} on {Date}", doctorId, date.Date);
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

