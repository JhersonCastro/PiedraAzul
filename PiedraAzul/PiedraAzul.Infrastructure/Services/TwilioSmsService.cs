using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PiedraAzul.Application.Common.Interfaces;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace PiedraAzul.Infrastructure.Services;

public class TwilioSmsService : IMessageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TwilioSmsService> _logger;
    private readonly bool _ready;

    public TwilioSmsService(IConfiguration configuration, ILogger<TwilioSmsService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var accountSid = _configuration["Twilio:AccountSid"];
        var authToken = _configuration["Twilio:AuthToken"];

        if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken))
        {
            _logger.LogWarning("Twilio credentials not configured — SMS will be disabled");
            return;
        }

        TwilioClient.Init(accountSid, authToken);
        _ready = true;
    }

    public async Task<bool> SMSAsync(string phoneNumber, string message)
    {
        if (!_ready)
        {
            _logger.LogWarning("SMS not sent to {PhoneNumber} — Twilio is not configured", phoneNumber);
            return false;
        }

        try
        {
            var fromNumber = _configuration["Twilio:PhoneNumber"];
            if (string.IsNullOrEmpty(fromNumber))
            {
                _logger.LogError("Twilio PhoneNumber not configured");
                return false;
            }

            var toNumber = phoneNumber.StartsWith("+") ? phoneNumber : $"+{phoneNumber}";

            var sms = await MessageResource.CreateAsync(
                body: message,
                from: new Twilio.Types.PhoneNumber(fromNumber),
                to: new Twilio.Types.PhoneNumber(toNumber)
            );

            _logger.LogInformation("SMS enviado exitosamente. SID: {SmsId}, To: {ToNumber}", sms.Sid, phoneNumber);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando SMS a {PhoneNumber}", phoneNumber);
            return false;
        }
    }
}
