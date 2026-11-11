
using System.ComponentModel.DataAnnotations;

namespace ClinicHub.DTOs.Admin
{
    public class EditAdminDto
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [Phone]
        [RegularExpression(
            "^[6-9]\\d{9}$",
            ErrorMessage = "Please enter a valid 10-digit Indian mobile number."
        )]
        public string Phone { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
