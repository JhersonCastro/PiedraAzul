namespace PiedraAzul.Domain.Entities.Profiles.Patients;

public enum VerificationSessionType
{
    Guest = 0,
    RegisteredUser = 1
}

public class GuestVerificationSession
{
    public string Hash { get; set; } = "";
    public VerificationSessionType SessionType { get; set; }

    // Referencia opcional al guest (FLUJO 3)
    public string? GuestId { get; set; }
    public GuestPatient? Guest { get; set; }

    // Referencia opcional al usuario registrado (FLUJO 2)
    public string? UserId { get; set; }

    // Datos del usuario (cuando es RegisteredUser, se copian aquí)
    public string? UserName { get; set; }
    public string? UserPhone { get; set; }
    public string? UserEmail { get; set; }

    // OTP
    public string? OtpCode { get; set; }
    public string? Channel { get; set; }
    public bool Verified { get; set; }

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public GuestVerificationSession() { }

    /// <summary>Constructor para sesiones de guest (FLUJO 3).</summary>
    public static GuestVerificationSession ForGuest(string guestId, int expirationMinutes)
    {
        return new GuestVerificationSession
        {
            Hash = Guid.NewGuid().ToString("N"),
            SessionType = VerificationSessionType.Guest,
            GuestId = guestId,
            Verified = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
        };
    }

    /// <summary>Constructor para sesiones de usuario registrado continuando como invitado (FLUJO 2).</summary>
    public static GuestVerificationSession ForRegisteredUser(
        string userId, string userName, string? userPhone, string? userEmail, int expirationMinutes)
    {
        return new GuestVerificationSession
        {
            Hash = Guid.NewGuid().ToString("N"),
            SessionType = VerificationSessionType.RegisteredUser,
            UserId = userId,
            UserName = userName,
            UserPhone = userPhone,
            UserEmail = userEmail,
            Verified = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
        };
    }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    public void SetOtp(string code, string channel)
    {
        OtpCode = code;
        Channel = channel;
    }

    public void MarkVerified() => Verified = true;

    /// <summary>Actualiza datos editados por el usuario después de verificar OTP.</summary>
    public void UpdateUserData(string? name, string? phone, string? email)
    {
        if (SessionType == VerificationSessionType.RegisteredUser)
        {
            if (!string.IsNullOrWhiteSpace(name)) UserName = name;
            if (!string.IsNullOrWhiteSpace(phone)) UserPhone = phone;
            if (!string.IsNullOrWhiteSpace(email)) UserEmail = email;
        }
    }
}
