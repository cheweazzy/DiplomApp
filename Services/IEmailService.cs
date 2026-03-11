using DiplomApp.Models;

namespace DiplomApp.Services
{
    public interface IEmailService
    {
        Task SendReservationConfirmationAsync(Reservation reservation, CancellationToken cancellationToken = default);
    }
}

