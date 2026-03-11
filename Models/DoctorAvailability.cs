using System.ComponentModel.DataAnnotations;

namespace DiplomApp.Models
{
    /// <summary>
    /// Represents doctor's availability for specific time slots
    /// </summary>
    public class DoctorAvailability
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string DoctorId { get; set; } = string.Empty; // Employee/Doctor User ID

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        /// <summary>
        /// List of available time slots for this day (stored as minutes from midnight)
        /// Example: 480 = 8:00, 510 = 8:30, 540 = 9:00, etc.
        /// </summary>
        public string AvailableSlots { get; set; } = string.Empty; // JSON array of time slots in minutes from midnight

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

