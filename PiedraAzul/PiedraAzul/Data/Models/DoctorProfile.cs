using PiedraAzul.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PiedraAzul.Data.Models
{
    public class DoctorProfile
    {
        [Key]
        [Required]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public DoctorType Specialty { get; set; }
        public string LicenseNumber { get; set; } = string.Empty;
        
        public string Notes { get; set; } = string.Empty;
    }
}
