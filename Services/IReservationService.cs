using DiplomApp.Models;

namespace DiplomApp.Services
{
    public interface IReservationService
    {
        Task<IReadOnlyList<Reservation>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Reservation>> GetReservationsByUserAsync(string userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Reservation>> GetReservationsBySpecialtyAsync(MedicalSpecialty specialty, CancellationToken cancellationToken = default);
        Task<Reservation> CreateAsync(Reservation reservation, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid reservationId, string? userId = null, CancellationToken cancellationToken = default);
        Task<List<DateTime>> GetAvailableTimeSlotsAsync(DateTime date, MedicalSpecialty? specialty = null, CancellationToken cancellationToken = default);
        Task<bool> IsTimeSlotAvailableAsync(DateTime slot, MedicalSpecialty? specialty = null, CancellationToken cancellationToken = default);
    }
}




