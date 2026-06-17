using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PiedraAzul.Application.Common.Interfaces;

namespace PiedraAzul.Infrastructure.Services;

/// <summary>
/// Envía mensajes de texto libre por WhatsApp Business Cloud API (Meta).
/// En modo desarrollo funciona sin templates para números de prueba registrados.
/// En producción también funciona dentro de la ventana de 24h de conversación activa.
/// </summary>
public class WhatsAppService : IWhatsAppService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;

    public WhatsAppService(IHttpClientFactory httpFactory, IConfiguration config)
    {
        _httpFactory = httpFactory;
        _config = config;
    }

    public async Task<bool> SendMessageAsync(string phoneE164, string message)
    {
        var accessToken   = _config["WhatsApp:AccessToken"]   ?? throw new InvalidOperationException("WhatsApp:AccessToken no configurado");
        var phoneNumberId = _config["WhatsApp:PhoneNumberId"] ?? throw new InvalidOperationException("WhatsApp:PhoneNumberId no configurado");
        var apiVersion    = _config["WhatsApp:ApiVersion"]    ?? "v25.0";

        var url = $"https://graph.facebook.com/{apiVersion}/{phoneNumberId}/messages";

        // Mensaje de texto libre — sin templates
        var payload = new
        {
            messaging_product = "whatsapp",
            to   = phoneE164,
            type = "text",
            text = new { body = message }
        };

        var json   = JsonSerializer.Serialize(payload);
        var client = _httpFactory.CreateClient("WhatsApp");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PostAsync(
            url,
            new StringContent(json, Encoding.UTF8, "application/json"));

        return response.IsSuccessStatusCode;
    }
}
