using PiedraAzul.Domain.Entities.Profiles.Patients;

namespace PiedraAzul.Application.Common.Interfaces;

public enum OtpChannel { WhatsApp, Email, SMS }

/// <summary>Datos del usuario verificado (puede ser guest o usuario registrado).</summary>
public record VerifiedUserData(
    string Id,
    string Name,
    string Phone,
    string? Email,
    VerificationSessionType SessionType
);

public record GuestVerificationResult(bool Success, VerifiedUserData? Data);

/// <summary>
/// Información de un invitado (guest) con la misma cédula que un usuario registrado,
/// candidato a merge. Incluye un hash de verificación listo para enviar OTP.
/// </summary>
public record MergeableGuestInfo(
    string GuestId,
    string GuestName,
    int AppointmentCount,
    string VerificationHash,
    string? Phone,
    string? Email
);

/// <summary>Resultado de un merge guest → cuenta registrada.</summary>
public record GuestMergeResult(bool Success, int MergedCount, string? Error);

public interface IGuestOtpService
{
    /// <summary>Genera un OTP, lo guarda en caché y lo envía. Devuelve un sessionToken opaco.</summary>
    Task<string> SendAsync(string phone, string? email, OtpChannel channel);

    /// <summary>Verifica el código. Retorna true si es válido y lo marca como usado.</summary>
    Task<bool> VerifyAsync(string sessionToken, string code);

    // ── Pre-verificación para guests existentes (FLUJO 3) ────────────────

    /// <summary>Crea una sesión de verificación persistente para un guest existente. Devuelve el hash.</summary>
    Task<string> CreateSessionAsync(string guestId, int expirationMinutes);

    // ── Pre-verificación para usuarios registrados continuando como invitado (FLUJO 2) ──

    /// <summary>Crea una sesión de verificación para un usuario registrado que continuará como invitado. Devuelve el hash.</summary>
    Task<string> CreateSessionForRegisteredUserAsync(
        string userId, string userName, string? userPhone, string? userEmail, int expirationMinutes);

    /// <summary>Genera el OTP para la sesión hash y lo envía al usuario por el canal indicado.</summary>
    Task SendOtpByHashAsync(string hash, OtpChannel channel);

    /// <summary>Verifica el código contra la sesión hash. Si es correcto, marca la sesión como verificada y devuelve los datos del usuario.</summary>
    Task<GuestVerificationResult> VerifyOtpByHashAsync(
        string hash, string code, string? updatedName = null, string? updatedPhone = null, string? updatedEmail = null);

    /// <summary>Comprueba si la sesión hash ya fue verificada (para omitir OTP final).</summary>
    Task<bool> IsSessionVerifiedAsync(string hash);

    /// <summary>Obtiene los datos verificados de una sesión (para uso en booking).</summary>
    Task<VerifiedUserData?> GetVerifiedDataAsync(string hash);

    // ── Merge guest → cuenta registrada ──────────────────────────────────

    /// <summary>
    /// Busca un invitado NO mergeado con la cédula dada que tenga al menos una cita
    /// transferible. Si existe, crea una sesión de verificación y devuelve su info.
    /// Retorna null si no hay candidato a merge.
    /// </summary>
    Task<MergeableGuestInfo?> GetMergeableGuestAsync(string identification);

    /// <summary>
    /// Verifica el OTP de la sesión hash y, si es correcto, transfiere las citas del
    /// invitado al usuario registrado y marca al invitado como mergeado. La cédula del
    /// invitado debe coincidir con la del usuario registrado (seguridad).
    /// </summary>
    Task<GuestMergeResult> MergeGuestAppointmentsAsync(
        string hash, string code, string registeredUserId, string registeredUserIdentification);
}
