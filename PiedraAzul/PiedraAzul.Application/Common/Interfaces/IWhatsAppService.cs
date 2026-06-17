namespace PiedraAzul.Application.Common.Interfaces;

/// <summary>
/// Servicio para enviar mensajes por WhatsApp.
/// Implementa esta interfaz con tu API de WhatsApp existente.
/// </summary>
public interface IWhatsAppService
{
    /// <summary>
    /// Envía un mensaje de texto al número colombiano indicado.
    /// El número debe estar en formato E.164: +57XXXXXXXXXX
    /// </summary>
    Task<bool> SendMessageAsync(string phoneE164, string message);
}
