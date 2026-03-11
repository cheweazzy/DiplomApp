using Microsoft.AspNetCore.Identity;

namespace DiplomApp.Models
{
    public class User : IdentityUser
    {
        public string? FullName { get; set; }
        
        /// <summary>
        /// Medical specialty for employees/doctors. Null for customers and admins.
        /// </summary>
        public MedicalSpecialty? MedicalSpecialty { get; set; }
    }
}

