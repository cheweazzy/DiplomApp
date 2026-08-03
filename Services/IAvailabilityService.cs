using DiplomApp.Models;

namespace DiplomApp.Services
{
    public interface IAvailabilityService
    {
        Task<DoctorAvailability?> GetAvailabilityAsync(string doctorId, DateTime date, CancellationToken cancellationToken = default);
        Task<List<DoctorAvailability>> GetAvailabilitiesAsync(string doctorId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
        Task<DoctorAvailability> SaveAvailabilityAsync(string doctorId, DateTime date, List<int> availableSlots, CancellationToken cancellationToken = default);
        Task<bool> DeleteAvailabilityAsync(string doctorId, DateTime date, CancellationToken cancellationToken = default);
        Task<bool> IsSlotAvailableAsync(string doctorId, DateTime slot, CancellationToken cancellationToken = default);
        Task<List<DateTime>> GetAvailableSlotsForDateAsync(string doctorId, DateTime date, CancellationToken cancellationToken = default);
    }
}

