namespace PiedraAzul.Application.Common.Interfaces;

public enum OtpChannel { WhatsApp, Email }

public interface IGuestOtpService
{
    /// <summary>
    /// Genera un OTP, lo guarda en caché y lo envía por el canal elegido.
    /// Devuelve un sessionToken opaco que se usa para verificar después.
    /// </summary>
    Task<string> SendAsync(string phone, string? email, OtpChannel channel);

    /// <summary>
    /// Verifica el código. Retorna true si es válido y lo marca como usado.
    /// Lanza excepción si expiró o se superaron los intentos.
    /// </summary>
    Task<bool> VerifyAsync(string sessionToken, string code);
}
