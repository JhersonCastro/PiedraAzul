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
        public string? PendingEmail { get; set; }
        public string? EmailChangeCode { get; set; }
        public DateTime? EmailChangeCodeExpiresAt { get; set; }

        public void SetEmailChangeToken(string newEmail, string code)
        {
            PendingEmail = newEmail;
            EmailChangeCode = code;
            EmailChangeCodeExpiresAt = DateTime.UtcNow.AddMinutes(10);
        }

        public bool HasPendingEmailChange(string email)
        {
            return PendingEmail == email && EmailChangeCodeExpiresAt > DateTime.UtcNow;
        }

        public bool VerifyEmailChangeCode(string code)
        {
            return EmailChangeCode == code && EmailChangeCodeExpiresAt > DateTime.UtcNow;
        }

        public void ClearEmailChangeToken()
        {
            PendingEmail = null;
            EmailChangeCode = null;
            EmailChangeCodeExpiresAt = null;
        }
    }
}