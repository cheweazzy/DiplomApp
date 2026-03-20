using System.ComponentModel.DataAnnotations;

namespace DiplomApp.Models
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "Imię i nazwisko są wymagane")]
        [Display(Name = "Imię i Nazwisko")]
        [FullNameValidation(ErrorMessage = "Imię i nazwisko musi składać się z dokładnie dwóch słów (imię i nazwisko)")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adres email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format adresu e-mail")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Numer telefonu jest wymagany")]
        [Display(Name = "Numer Telefonu")]
        [PolishPhoneNumberValidation(ErrorMessage = "Numer telefonu musi składać się z dokładnie 9 cyfr (format polskie)")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hasło jest wymagane")]
        [Display(Name = "Hasło")]
        [PasswordValidation(ErrorMessage = "Hasło musi składać się z co najmniej 8 znaków, zawierać co najmniej jedną dużą literę, jedną cyfrę i jeden znak specjalny")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Potwierdzenie hasła jest wymagane")]
        [Display(Name = "Potwierdzenie Hasła")]
        [Compare(nameof(Password), ErrorMessage = "Hasła nie pasują")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Typ konta jest wymagany")]
        [Display(Name = "Typ Konta")]
        public string Role { get; set; } = "Customer";

        public string? AdminKey { get; set; }
        
        public string? EmployeeKey { get; set; }
        
        [Display(Name = "Specjalizacja Pracownika")]
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

