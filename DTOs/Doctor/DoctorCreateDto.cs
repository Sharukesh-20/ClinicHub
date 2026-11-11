
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicHub.DTOs.Doctor
{
    public class DoctorCreateDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        [RegularExpression(
            "^[6-9]\\d{9}$",
            ErrorMessage = "Please enter a valid 10-digit Indian mobile number."
        )]
        public string? Phone { get; set; }

        [Required]
        public int SpecialtyId { get; set; }

        [Range(0, 60)]
        public int YearsOfExperience { get; set; }

        public string? Biography { get; set; }

        [Required]
        [RegularExpression(
            "^(?=.*[A-Z])(?=.*[a-z])(?=.*\\d)(?=.*[^A-Za-z0-9]).{6,10}$",
            ErrorMessage = "Password must be 6-10 characters and contain an uppercase letter, lowercase letter, number, and special character."
        )]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
