
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicHub.DTOs.Doctor.DoctorProfile
{
    public class DoctorProfileEditDto
    {
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [RegularExpression("^[6-9][0-9]{9}$",
            ErrorMessage = "Enter a valid 10-digit Indian mobile number.")]
        public string? Phone { get; set; }

        public string? Biography { get; set; }

        public int? YearsOfExperience { get; set; }

        public string? ImagePath { get; set; }

        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}
