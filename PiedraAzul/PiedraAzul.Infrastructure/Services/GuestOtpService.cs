using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Domain.Entities.Profiles.Patients;
using PiedraAzul.Infrastructure.Persistence;

namespace PiedraAzul.Infrastructure.Services;

public class GuestOtpService : IGuestOtpService
{
    private readonly IMemoryCache _cache;
    private readonly IEmailService _emailService;
    private readonly IWhatsAppService _whatsApp;
    private readonly IMessageService _smsService;
    private readonly IConfiguration _config;
    private readonly ILogger<GuestOtpService> _logger;
    private readonly AppDbContext _context;

    private const int MaxAttempts = 3;

    private record OtpEntry(string Code, int Attempts, DateTime ExpiresAt);

    public GuestOtpService(
        IMemoryCache cache,
        IEmailService emailService,
        IWhatsAppService whatsApp,
        IMessageService smsService,
        IConfiguration config,
        ILogger<GuestOtpService> logger,
        AppDbContext context)
    {
        _cache = cache;
        _emailService = emailService;
        _whatsApp = whatsApp;
        _smsService = smsService;
        _config = config;
        _logger = logger;
        _context = context;
    }

    // ── Flujo nuevo usuario (sin cuenta previa) ─────────────────────────

    public async Task<string> SendAsync(string phone, string? email, OtpChannel channel)
    {
        var expirationMinutes = _config.GetValue<int>("Security:MFA:OTPExpirationMinutes", 10);
        var code = GenerateCode();
        var sessionToken = Guid.NewGuid().ToString("N");

        _logger.LogInformation("[GuestOTP] Generado código: {Code}, Token: {Token}, Channel: {Channel}", code, sessionToken, channel);

        var entry = new OtpEntry(code, 0, DateTime.UtcNow.AddMinutes(expirationMinutes));
        _cache.Set(CacheKey(sessionToken), entry, TimeSpan.FromMinutes(expirationMinutes + 1));

        await SendCodeAsync(channel, phone, email, code, expirationMinutes);

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

        _cache.Remove(key);
        return Task.FromResult(true);
    }

    // ── Pre-verificación para guests existentes (FLUJO 3) ───────────────

    public async Task<string> CreateSessionAsync(string guestId, int expirationMinutes)
    {
        var expMinutes = expirationMinutes > 0
            ? expirationMinutes
            : _config.GetValue<int>("Security:MFA:OTPExpirationMinutes", 10);

        var session = GuestVerificationSession.ForGuest(guestId, expMinutes);
        await _context.GuestVerificationSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        _logger.LogInformation("[GuestVerification] Sesión creada para guest {GuestId}, hash: {Hash}", guestId, session.Hash);
        return session.Hash;
    }

    // ── Pre-verificación para usuarios registrados (FLUJO 2) ────────────

    public async Task<string> CreateSessionForRegisteredUserAsync(
        string userId, string userName, string? userPhone, string? userEmail, int expirationMinutes)
    {
        var expMinutes = expirationMinutes > 0
            ? expirationMinutes
            : _config.GetValue<int>("Security:MFA:OTPExpirationMinutes", 10);

        var session = GuestVerificationSession.ForRegisteredUser(userId, userName, userPhone, userEmail, expMinutes);
        await _context.GuestVerificationSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        _logger.LogInformation("[GuestVerification] Sesión creada para usuario registrado {UserId}, hash: {Hash}", userId, session.Hash);
        return session.Hash;
    }

    // ── Envío de OTP por hash (compartido FLUJO 2 y FLUJO 3) ────────────

    public async Task SendOtpByHashAsync(string hash, OtpChannel channel)
    {
        var session = await _context.GuestVerificationSessions
            .Include(s => s.Guest)
            .FirstOrDefaultAsync(s => s.Hash == hash);

        if (session is null || session.IsExpired)
            throw new InvalidOperationException("Sesión de verificación inválida o expirada.");

        var expirationMinutes = _config.GetValue<int>("Security:MFA:OTPExpirationMinutes", 10);
        var code = GenerateCode();

        session.SetOtp(code, channel.ToString());
        await _context.SaveChangesAsync();

        // Obtener datos según el tipo de sesión
        string? phone;
        string? email;
        if (session.SessionType == VerificationSessionType.Guest)
        {
            if (session.Guest is null)
                throw new InvalidOperationException("Datos del guest no disponibles.");
            phone = session.Guest.Phone;
            email = session.Guest.Email;
        }
        else
        {
            phone = session.UserPhone;
            email = session.UserEmail;
        }

        if (channel == OtpChannel.Email && string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Este usuario no tiene email registrado.");

        if ((channel == OtpChannel.SMS || channel == OtpChannel.WhatsApp) && string.IsNullOrWhiteSpace(phone))
            throw new InvalidOperationException("Este usuario no tiene teléfono registrado.");

        var emailToSend = channel == OtpChannel.Email ? email : null;
        await SendCodeAsync(channel, phone ?? "", emailToSend, code, expirationMinutes);

        _logger.LogInformation("[GuestVerification] OTP enviado para hash {Hash}, canal {Channel}", hash, channel);
    }

    public async Task<GuestVerificationResult> VerifyOtpByHashAsync(
        string hash, string code, string? updatedName = null, string? updatedPhone = null, string? updatedEmail = null)
    {
        var session = await _context.GuestVerificationSessions
            .Include(s => s.Guest)
            .FirstOrDefaultAsync(s => s.Hash == hash);

        if (session is null || session.IsExpired)
            throw new InvalidOperationException("Sesión de verificación inválida o expirada.");

        // Si ya está verificada, retornar los datos actuales
        if (session.Verified)
        {
            return new GuestVerificationResult(true, BuildVerifiedData(session));
        }

        if (session.OtpCode != code.Trim())
            return new GuestVerificationResult(false, null);

        session.MarkVerified();

        // Aplicar actualizaciones si se proporcionaron
        if (!string.IsNullOrWhiteSpace(updatedName) || !string.IsNullOrWhiteSpace(updatedPhone) || !string.IsNullOrWhiteSpace(updatedEmail))
        {
            if (session.SessionType == VerificationSessionType.Guest && session.Guest is not null)
            {
                session.Guest.UpdateInfo(
                    updatedName ?? session.Guest.Name,
                    updatedPhone ?? session.Guest.Phone,
                    updatedEmail ?? session.Guest.Email);
            }
            else if (session.SessionType == VerificationSessionType.RegisteredUser)
            {
                session.UpdateUserData(updatedName, updatedPhone, updatedEmail);
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("[GuestVerification] Verificación exitosa para hash {Hash}", hash);
        return new GuestVerificationResult(true, BuildVerifiedData(session));
    }

    public async Task<bool> IsSessionVerifiedAsync(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash)) return false;
        var now = DateTime.UtcNow;
        return await _context.GuestVerificationSessions
            .AnyAsync(s => s.Hash == hash && s.Verified && s.ExpiresAt > now);
    }

    public async Task<VerifiedUserData?> GetVerifiedDataAsync(string hash)
    {
        var session = await _context.GuestVerificationSessions
            .Include(s => s.Guest)
            .FirstOrDefaultAsync(s => s.Hash == hash);

        if (session is null || !session.Verified || session.IsExpired)
            return null;

        return BuildVerifiedData(session);
    }

    // ── Merge guest → cuenta registrada ──────────────────────────────────

    public async Task<MergeableGuestInfo?> GetMergeableGuestAsync(string identification)
    {
        if (string.IsNullOrWhiteSpace(identification)) return null;
        var id = identification.Trim();

        var guest = await _context.Patients
            .OfType<GuestPatient>()
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id && g.MergedToUserId == null);

        if (guest is null) return null;

        // Transferimos TODAS las citas del invitado (historial incluido) para que la
        // cuenta registrada conserve su historial completo.
        var apptCount = await _context.Appointments
            .CountAsync(a => a.PatientGuestId == guest.Id);

        if (apptCount == 0) return null;

        var hash = await CreateSessionAsync(guest.Id, expirationMinutes: 0);

        return new MergeableGuestInfo(
            GuestId: guest.Id,
            GuestName: guest.Name,
            AppointmentCount: apptCount,
            VerificationHash: hash,
            Phone: guest.Phone,
            Email: guest.Email);
    }

    public async Task<GuestMergeResult> MergeGuestAppointmentsAsync(
        string hash, string code, string registeredUserId, string registeredUserIdentification)
    {
        var session = await _context.GuestVerificationSessions
            .Include(s => s.Guest)
            .FirstOrDefaultAsync(s => s.Hash == hash);

        if (session is null || session.IsExpired)
            return new GuestMergeResult(false, 0, "Sesión de verificación inválida o expirada.");

        if (session.SessionType != VerificationSessionType.Guest || session.Guest is null)
            return new GuestMergeResult(false, 0, "Sesión de verificación inválida para vincular.");

        // Seguridad: la cédula del invitado debe coincidir con la del usuario autenticado.
        if (!string.Equals(session.Guest.Id, registeredUserIdentification?.Trim(), StringComparison.OrdinalIgnoreCase))
            return new GuestMergeResult(false, 0, "La cuenta invitada no corresponde a tu cédula.");

        if (session.Guest.IsMerged)
            return new GuestMergeResult(false, 0, "Esta cuenta invitada ya fue vinculada.");

        if (!session.Verified)
        {
            if (session.OtpCode != code.Trim())
                return new GuestMergeResult(false, 0, "Código incorrecto. Intenta de nuevo.");
            session.MarkVerified();
        }

        // Transferir TODAS las citas del invitado al usuario registrado.
        var appointments = await _context.Appointments
            .Where(a => a.PatientGuestId == session.Guest.Id)
            .ToListAsync();

        foreach (var appt in appointments)
            appt.ReassignToRegisteredUser(registeredUserId);

        session.Guest.MarkAsMerged(registeredUserId);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "[GuestMerge] Guest {GuestId} vinculado a usuario {UserId}. Citas transferidas: {Count}",
            session.Guest.Id, registeredUserId, appointments.Count);

        return new GuestMergeResult(true, appointments.Count, null);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static VerifiedUserData? BuildVerifiedData(GuestVerificationSession session)
    {
        if (session.SessionType == VerificationSessionType.Guest)
        {
            if (session.Guest is null) return null;
            return new VerifiedUserData(
                Id: session.Guest.Id,
                Name: session.Guest.Name,
                Phone: session.Guest.Phone,
                Email: session.Guest.Email,
                SessionType: VerificationSessionType.Guest
            );
        }
        else
        {
            return new VerifiedUserData(
                Id: session.UserId ?? "",
                Name: session.UserName ?? "",
                Phone: session.UserPhone ?? "",
                Email: session.UserEmail,
                SessionType: VerificationSessionType.RegisteredUser
            );
        }
    }

    private async Task SendCodeAsync(OtpChannel channel, string phone, string? email, string code, int expirationMinutes)
    {
        if (channel == OtpChannel.WhatsApp)
        {
            var phoneE164 = ToE164Colombia(phone);
            var sent = await _whatsApp.SendOtpAsync(phoneE164, code);
            if (!sent)
                throw new InvalidOperationException("No se pudo enviar el WhatsApp. Intenta con otro canal.");
        }
        else if (channel == OtpChannel.SMS)
        {
            var phoneE164 = ToE164Colombia(phone);
            var message = $"Tu código de confirmación para tu cita en Piedra Azul es: {code}. Válido por {expirationMinutes} minutos.";
            var sent = await _smsService.SMSAsync(phoneE164, message);
            if (!sent)
                throw new InvalidOperationException("No se pudo enviar el SMS. Intenta con otro canal.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new InvalidOperationException("Email requerido para canal Email");
            await _emailService.SendMFAEmailAsync(email, "Paciente", code, expirationMinutes);
        }
    }

    private static string GenerateCode() => new Random().Next(100_000, 999_999).ToString();

    public static string ToE164Colombia(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("57") && digits.Length == 12)
            return digits;

        if (digits.Length == 10 && digits.StartsWith("3"))
            return $"57{digits}";

        throw new ArgumentException($"Número colombiano inválido: {phone}");
    }

    private static string CacheKey(string token) => $"guest_otp:{token}";
}
