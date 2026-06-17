using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PiedraAzul.Application.Common.Interfaces;

namespace PiedraAzul.Infrastructure.Services;

public class GuestOtpService : IGuestOtpService
{
    private readonly IMemoryCache _cache;
    private readonly IEmailService _emailService;
    private readonly IWhatsAppService _whatsApp;
    private readonly IConfiguration _config;
    private readonly ILogger<GuestOtpService> _logger;

    private const int MaxAttempts = 3;

    private record OtpEntry(string Code, int Attempts, DateTime ExpiresAt);

    public GuestOtpService(
        IMemoryCache cache,
        IEmailService emailService,
        IWhatsAppService whatsApp,
        IConfiguration config,
        ILogger<GuestOtpService> logger)
    {
        _cache = cache;
        _emailService = emailService;
        _whatsApp = whatsApp;
        _config = config;
        _logger = logger;
    }

    public async Task<string> SendAsync(string phone, string? email, OtpChannel channel)
    {
        var expirationMinutes = _config.GetValue<int>("Security:MFA:OTPExpirationMinutes", 10);
        var code = GenerateCode();
        var sessionToken = Guid.NewGuid().ToString("N");

        _logger.LogInformation("[GuestOTP] Generado código: {Code}, Token: {Token}, Channel: {Channel}", code, sessionToken, channel);

        var entry = new OtpEntry(code, 0, DateTime.UtcNow.AddMinutes(expirationMinutes));
        _cache.Set(CacheKey(sessionToken), entry, TimeSpan.FromMinutes(expirationMinutes + 1));

        if (channel == OtpChannel.WhatsApp)
        {
            var phoneE164 = ToE164Colombia(phone);
            var message = $"Tu código de confirmación para tu cita en Piedra Azul es: *{code}*\nVálido por {expirationMinutes} minutos.";
            _logger.LogInformation("[GuestOTP] Enviando mensaje WhatsApp a {Phone}: {Message}", phoneE164, message);
            await _whatsApp.SendMessageAsync(phoneE164, message);
            _logger.LogInformation("[GuestOTP] Mensaje WhatsApp enviado a {Phone}", phoneE164);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new InvalidOperationException("Email requerido para canal Email");

            _logger.LogInformation("[GuestOTP] Enviando email a {Email} con código {Code}", email, code);
            await _emailService.SendMFAEmailAsync(email, "Paciente", code, expirationMinutes);
            _logger.LogInformation("[GuestOTP] Email enviado a {Email}", email);
        }

        return sessionToken;
    }

    public Task<bool> VerifyAsync(string sessionToken, string code)
    {
        var key = CacheKey(sessionToken);

        if (!_cache.TryGetValue(key, out OtpEntry? entry) || entry is null)
            throw new InvalidOperationException("El código expiró o no existe.");

        if (DateTime.UtcNow > entry.ExpiresAt)
        {
            _cache.Remove(key);
            throw new InvalidOperationException("El código expiró.");
        }

        if (entry.Attempts >= MaxAttempts)
        {
            _cache.Remove(key);
            throw new InvalidOperationException("Demasiados intentos fallidos. Solicita un nuevo código.");
        }

        if (entry.Code != code.Trim())
        {
            var updated = entry with { Attempts = entry.Attempts + 1 };
            _cache.Set(key, updated, entry.ExpiresAt - DateTime.UtcNow);
            return Task.FromResult(false);
        }

        // Código correcto — lo invalida inmediatamente
        _cache.Remove(key);
        return Task.FromResult(true);
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static string GenerateCode()
    {
        var random = new Random();
        return random.Next(100_000, 999_999).ToString();
    }

    /// <summary>
    /// Convierte un número colombiano (10 dígitos que empiezan con 3)
    /// al formato E.164: +57XXXXXXXXXX
    /// </summary>
    public static string ToE164Colombia(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        // Si ya tiene código de país 57
        if (digits.StartsWith("57") && digits.Length == 12)
            return $"{digits}";

        // Número local colombiano de 10 dígitos
        if (digits.Length == 10 && digits.StartsWith("3"))
            return $"57{digits}";

        throw new ArgumentException($"Número colombiano inválido: {phone}");
    }

    private static string CacheKey(string token) => $"guest_otp:{token}";
}
