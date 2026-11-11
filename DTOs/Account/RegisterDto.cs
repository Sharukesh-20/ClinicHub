
using System.ComponentModel.DataAnnotations;

namespace ClinicHub.DTOs.Account
{
    public class RegisterDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        [RegularExpression(
            "^[6-9][0-9]{9}$",
            ErrorMessage = "Enter a valid 10-digit Indian mobile number."
        )]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public DateOnly DateOfBirth { get; set; }

        [Required]
        public string Gender { get; set; } = string.Empty;

        [Required]
        /*
            (?=...) : Checks that something exists without consuming it.
            . : Matches any character.
            [^A-Za-z0-9] : Requires a special character.
        */
        [RegularExpression(
            "^(?=.*[A-Z])(?=.*[a-z])(?=.*\\d)(?=.*[^A-Za-z0-9]).{6,10}$",
            ErrorMessage = "Password must be 6-10 characters and contain an uppercase letter, lowercase letter, number, and special character."
        )]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
