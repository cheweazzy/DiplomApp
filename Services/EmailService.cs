using DiplomApp.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DiplomApp.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendReservationConfirmationAsync(Reservation reservation, CancellationToken cancellationToken = default)
        {
            // Skip sending email if no email address is provided
            if (string.IsNullOrWhiteSpace(reservation.Email))
            {
                _logger.LogWarning("Nie można wysłać potwierdzenia rezerwacji dla rezerwacji {ReservationId}: brak adresu email", reservation.Id);
                return;
            }

            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");
                var smtpHost = emailSettings["SmtpHost"];
                var smtpPort = emailSettings.GetValue<int>("SmtpPort", 587);
                var smtpUsername = emailSettings["SmtpUsername"];
                var smtpPassword = emailSettings["SmtpPassword"];
                var fromEmail = emailSettings["FromEmail"] ?? smtpUsername;
                var fromName = emailSettings["FromName"] ?? "Gabinet Lekarski";

                // Validate required settings
                if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUsername) || string.IsNullOrWhiteSpace(smtpPassword))
                {
                    _logger.LogWarning("Ustawienia emaila nie są skonfigurowane. Pomijanie wysyłania emaila dla rezerwacji {ReservationId}", reservation.Id);
                    return;
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(new MailboxAddress(reservation.Name, reservation.Email));
                message.Subject = "Potwierdzenie wizyty - Gabinet Lekarski";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = GenerateEmailBody(reservation)
                };

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls, cancellationToken);
                await client.AuthenticateAsync(smtpUsername, smtpPassword, cancellationToken);
                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);

                _logger.LogInformation("Potwierdzenie emaila wysłane pomyślnie dla rezerwacji {ReservationId} do {Email}", 
                    reservation.Id, reservation.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas wysyłania potwierdzenia emaila dla rezerwacji {ReservationId} do {Email}", 
                    reservation.Id, reservation.Email);
                // Don't throw - we don't want email failures to break the reservation process
            }
        }

        private string GenerateEmailBody(Reservation reservation)
        {
            // Slots are now 30 minutes long
            var endTime = reservation.ReservationDateTime.AddMinutes(30);
            var dateStr = reservation.ReservationDateTime.ToString("dd.MM.yyyy");
            var startTimeStr = reservation.ReservationDateTime.ToString("HH:mm");
            var endTimeStr = endTime.ToString("HH:mm");

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #d32f2f; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
        .info-box {{ background-color: white; padding: 15px; margin: 15px 0; border-left: 4px solid #d32f2f; border-radius: 3px; }}
        .info-label {{ font-weight: bold; color: #666; }}
        .footer {{ text-align: center; margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Potwierdzenie Rezerwacji</h1>
        </div>
        <div class=""content"">
            <p>Witaj {reservation.Name},</p>
            <p>Twoja rezerwacja została pomyślnie utworzona!</p>
            
            <div class=""info-box"">
                <div><span class=""info-label"">Numer rezerwacji:</span> {reservation.Id}</div>
                <div><span class=""info-label"">Data:</span> {dateStr}</div>
                <div><span class=""info-label"">Godzina:</span> {startTimeStr} - {endTimeStr}</div>
                <div><span class=""info-label"">Specjalista:</span> {reservation.MedicalSpecialty}</div>
                {(!string.IsNullOrWhiteSpace(reservation.Notes) ? $"<div><span class=\"info-label\">Uwagi:</span> {reservation.Notes}</div>" : "")}
            </div>

            <p>Prosimy o przybycie 10 minut przed rozpoczęciem wizyty.</p>
            <p>W razie pytań lub potrzeby anulowania wizyty, prosimy o kontakt.</p>
            
            <div class=""footer"">
                <p>Gabinet Lekarski</p>
                <p>Dziękujemy za wybór naszych usług!</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }
    }
}

