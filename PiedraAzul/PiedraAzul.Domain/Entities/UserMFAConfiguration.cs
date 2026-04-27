namespace PiedraAzul.Domain.Entities;

public class UserMFAConfiguration
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string MFAMethod { get; set; } = string.Empty; // "Email" or "TOTP"
    public string? BackupCodesEncrypted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }

    public UserMFAConfiguration()
    {
    }

    public UserMFAConfiguration(string userId, string mfaMethod)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        MFAMethod = mfaMethod;
        IsEnabled = false;
        CreatedAt = DateTime.UtcNow;
    }
}
