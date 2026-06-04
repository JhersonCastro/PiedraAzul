using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PiedraAzul.Application.Common.Interfaces;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace PiedraAzul.Infrastructure.Services;

/// <summary>
/// Envía mensajes por WhatsApp a través de Twilio (mismo Account que SMS).
/// Para OTP usa una plantilla aprobada (ContentSid); para texto libre usa Body,
/// válido solo dentro de la ventana de sesión de 24h de Twilio.
/// </summary>
public class WhatsAppService : IWhatsAppService
{
    private readonly IConfiguration _config;
    private readonly ILogger<WhatsAppService> _logger;
    private readonly bool _ready;

    public WhatsAppService(IConfiguration config, ILogger<WhatsAppService> logger)
    {
        _config = config;
        _logger = logger;

        var accountSid = _config["Twilio:AccountSid"];
        var authToken = _config["Twilio:AuthToken"];

        if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken))
        {
            _logger.LogWarning("Twilio credentials not configured — WhatsApp will be disabled");
            return;
        }

        TwilioClient.Init(accountSid, authToken);
        _ready = true;
    }

    public async Task<bool> SendOtpAsync(string phoneE164, string code)
    {
        var contentSid = _config["Twilio:WhatsAppOtpContentSid"];
        if (string.IsNullOrEmpty(contentSid))
        {
            _logger.LogError("Twilio:WhatsAppOtpContentSid no configurado");
            return false;
        }

        var variables = JsonSerializer.Serialize(new Dictionary<string, string> { ["1"] = code });

        return await SendAsync(phoneE164, opts =>
        {
            opts.ContentSid = contentSid;
            opts.ContentVariables = variables;
        });
    }

    public async Task<bool> SendMessageAsync(string phoneE164, string message)
        => await SendAsync(phoneE164, opts => opts.Body = message);

    private async Task<bool> SendAsync(string phoneE164, Action<CreateMessageOptions> configure)
    {
        if (!_ready)
        {
            _logger.LogWarning("WhatsApp not sent to {Phone} — Twilio is not configured", phoneE164);
            return false;
        }

        var fromNumber = _config["Twilio:WhatsAppNumber"];
        if (string.IsNullOrEmpty(fromNumber))
        {
            _logger.LogError("Twilio:WhatsAppNumber no configurado");
            return false;
        }

        try
        {
            var to = phoneE164.StartsWith("whatsapp:")
                ? phoneE164
                : $"whatsapp:{(phoneE164.StartsWith("+") ? phoneE164 : $"+{phoneE164}")}";

            var options = new CreateMessageOptions(new Twilio.Types.PhoneNumber(to))
            {
                From = new Twilio.Types.PhoneNumber(fromNumber)
            };
            configure(options);

            var message = await MessageResource.CreateAsync(options);
            _logger.LogInformation("WhatsApp enviado exitosamente. SID: {MessageId}, To: {ToNumber}", message.Sid, phoneE164);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando WhatsApp a {Phone}", phoneE164);
            return false;
        }
    }
}
