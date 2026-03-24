using Microsoft.AspNetCore.Identity;
using PiedraAzul.Data.Models;
using PiedraAzul.Shared.Enums;

namespace PiedraAzul.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string IdentificationNumber { get; set; } = string.Empty;
        public DateTime created_at { get; init; } = DateTime.UtcNow;
        public GenderType Gender { get; set; } = GenderType.NonSpecified;
        public DateTime? BirthDate { get; set; }
        public string Name { get; set; }
        public string AvatarUrl { get; set; } = "default.png";

        public virtual PatientProfile? PatientProfile{ get; set; }
        public virtual DoctorProfile? DoctorProfile{ get; set; }
    }
}
