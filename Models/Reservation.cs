using System.ComponentModel.DataAnnotations;

namespace DiplomApp.Models
{
    public class Reservation
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(200)]
        public string? Email { get; set; }

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime ReservationDateTime { get; set; } = DateTime.Now.AddHours(1);

        [Required]
        public MedicalSpecialty MedicalSpecialty { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}




