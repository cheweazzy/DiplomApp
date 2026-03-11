using System.ComponentModel.DataAnnotations;

namespace DiplomApp.Models
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "Full Name is required")]
        [Display(Name = "Full Name")]
        [FullNameValidation(ErrorMessage = "Full Name must consist of exactly two words (first name and last name)")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone Number is required")]
        [Display(Name = "Phone Number")]
        [PolishPhoneNumberValidation(ErrorMessage = "Phone number must be exactly 9 digits (Polish format)")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [Display(Name = "Password")]
        [PasswordValidation(ErrorMessage = "Password must be at least 8 characters long, contain at least one uppercase letter, one digit, and one special character")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm Password is required")]
        [Display(Name = "Confirm Password")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Account Type is required")]
        [Display(Name = "Account Type")]
        public string Role { get; set; } = "Customer";

        public string? AdminKey { get; set; }
        
        public string? EmployeeKey { get; set; }
        
        [Display(Name = "Medical Specialty")]
        public MedicalSpecialty? EmployeeMedicalSpecialty { get; set; }
    }

    // Custom validation attribute for Full Name (exactly two words)
    public class FullNameValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return false;

            var fullName = value.ToString()!.Trim();
            var words = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            return words.Length == 2;
        }
    }

    // Custom validation attribute for Polish phone numbers (exactly 9 digits)
    public class PolishPhoneNumberValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return false;

            var phoneNumber = value.ToString()!.Trim();
            
            // Remove any spaces, dashes, or other characters
            phoneNumber = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            
            // Check if it's exactly 9 digits
            return phoneNumber.Length == 9 && phoneNumber.All(char.IsDigit);
        }
    }

    // Custom validation attribute for password strength
    public class PasswordValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return false;

            var password = value.ToString()!;
            
            // Check minimum length
            if (password.Length < 8)
                return false;

            // Check for at least one uppercase letter
            if (!password.Any(char.IsUpper))
                return false;

            // Check for at least one digit
            if (!password.Any(char.IsDigit))
                return false;

            // Check for at least one special character
            if (!password.Any(c => !char.IsLetterOrDigit(c)))
                return false;

            return true;
        }
    }
}

