using Microsoft.AspNetCore.Identity;
using PiedraAzul.Domain.Entities.Shared.Enums;

namespace PiedraAzul.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string IdentificationNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public GenderType Gender { get; set; } = GenderType.NonSpecified;
        public DateTime? BirthDate { get; set; }
        public string Name { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = "default.png";
    }
}