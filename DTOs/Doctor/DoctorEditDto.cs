using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicHub.DTOs.Doctor
{
    public class DoctorEditDto
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [RegularExpression("^[6-9][0-9]{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number.")]
        public string? Phone { get; set; }

        [Required]
        public int SpecialtyId { get; set; }
        public int? YearsOfExperience { get; set; }
        public string? Biography { get; set; }

        // images currently are stored in the wwwroot/images/doctors folder, and the path is stored in the database
        public string? IconOrImagePath { get; set; }

        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}
