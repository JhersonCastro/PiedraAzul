namespace PiedraAzul.Application.Common.Interfaces;

/// <summary>
/// Servicio para enviar mensajes por WhatsApp a través de Twilio.
/// </summary>
public interface IWhatsAppService
{
    /// <summary>
    /// Envía un código OTP por WhatsApp usando la plantilla aprobada de Twilio.
    /// El número puede ir con o sin '+'; se normaliza a formato E.164.
    /// </summary>
    Task<bool> SendOtpAsync(string phoneE164, string code);

    /// <summary>
    /// Envía un mensaje de texto libre por WhatsApp (válido dentro de la ventana
    /// de sesión de 24h de Twilio). El número debe estar en formato E.164.
    /// </summary>
    Task<bool> SendMessageAsync(string phoneE164, string message);
}
