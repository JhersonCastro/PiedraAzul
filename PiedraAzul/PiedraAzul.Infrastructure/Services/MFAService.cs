using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using OtpNet;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Domain.Entities;
using PiedraAzul.Infrastructure.Persistence;

namespace PiedraAzul.Infrastructure.Services;

public class MFAService : IMFAService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private const int BackupCodeCount = 10;
    private const int BackupCodeLength = 8;

    public MFAService(
        AppDbContext context,
        IEmailService emailService,
        IMemoryCache cache,
        IConfiguration configuration)
    {
        _context = context;
        _emailService = emailService;
        _cache = cache;
        _configuration = configuration;
    }

    public async Task<bool> IsEnabledAsync(string userId)
    {
        return await _context.UserMFAConfigurations
            .AnyAsync(m => m.UserId == userId && m.IsEnabled);
    }

    public async Task<string> GetMFAMethodAsync(string userId)
    {
        var mfa = await _context.UserMFAConfigurations
            .Where(m => m.UserId == userId && m.IsEnabled)
            .FirstOrDefaultAsync();

        return mfa?.MFAMethod ?? "Email";
    }

    public async Task<MFAStatus> GetMFAStatusAsync(string userId)
    {
        var mfas = await _context.UserMFAConfigurations
            .Where(m => m.UserId == userId)
            .ToListAsync();

        var emailOtpEnabled = mfas.Any(m => m.IsEnabled && m.MFAMethod == "Email");
        var totpEnabled = mfas.Any(m => m.IsEnabled && m.MFAMethod == "TOTP");
        var hasBackupCodes = mfas.Any(m => !string.IsNullOrEmpty(m.BackupCodesEncrypted));

        return new MFAStatus(
            EmailOTPEnabled: emailOtpEnabled,
            TOTPEnabled: totpEnabled,
            HasBackupCodes: hasBackupCodes
        );
    }

    public async Task<List<string>> EnableMFAAsync(string userId, string method)
    {
        // Desactivar todos los otros métodos
        var otherMfas = await _context.UserMFAConfigurations
            .Where(m => m.UserId == userId && m.MFAMethod != method && m.MFAMethod != "BackupCodes")
            .ToListAsync();

        foreach (var config in otherMfas)
            config.IsEnabled = false;

        // Activar el método solicitado
        var mfa = await _context.UserMFAConfigurations
            .FirstOrDefaultAsync(m => m.UserId == userId && m.MFAMethod == method);

        if (mfa is null)
        {
            mfa = new UserMFAConfiguration(userId, method);
            await _context.UserMFAConfigurations.AddAsync(mfa);
        }

        mfa.IsEnabled = true;
        mfa.CreatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Generar backup codes SIEMPRE al activar cualquier método
        var backupCodes = await GenerateBackupCodesAsync(userId);
        return backupCodes;
    }

    public async Task<bool> DisableMFAAsync(string userId, string method = "")
    {
        IQueryable<UserMFAConfiguration> query = _context.UserMFAConfigurations
            .Where(m => m.UserId == userId);

        if (!string.IsNullOrEmpty(method))
            query = query.Where(m => m.MFAMethod == method);

        var mfas = await query.ToListAsync();

        if (mfas.Count == 0)
            return false;

        foreach (var mfa in mfas)
            mfa.IsEnabled = false;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> VerifyOTPAsync(string userId, string otp)
    {
        var cacheKey = $"mfa_otp_{userId}";
        if (!_cache.TryGetValue(cacheKey, out string? storedOtp))
            return false;

        var isValid = storedOtp == otp;

        if (isValid)
        {
            _cache.Remove(cacheKey);
            var mfa = await _context.UserMFAConfigurations
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (mfa is not null)
            {
                mfa.LastUsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        return isValid;
    }

    public async Task<string> GenerateOTPAsync(string userId)
    {
        var otp = new Random().Next(100000, 999999).ToString();
        var expirationMinutes = _configuration.GetValue<int>("Security:MFA:OTPExpirationMinutes", 10);
        _cache.Set($"mfa_otp_{userId}", otp, TimeSpan.FromMinutes(expirationMinutes));

        return otp;
    }

    public async Task<string> GenerateTOTPSecretAsync(string userId)
    {
        var secret = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(secret);

        var mfa = await _context.UserMFAConfigurations
            .FirstOrDefaultAsync(m => m.UserId == userId && m.MFAMethod == "TOTP");

        if (mfa is null)
        {
            mfa = new UserMFAConfiguration(userId, "TOTP");
            await _context.UserMFAConfigurations.AddAsync(mfa);
        }

        mfa.TOTPSecret = base32Secret;
        _cache.Set($"totp_setup_{userId}", base32Secret, TimeSpan.FromMinutes(15));

        await _context.SaveChangesAsync();
        return base32Secret;
    }

    public async Task<string> GetTOTPQRCodeAsync(string userId, string email)
    {
        var secret = _cache.Get<string>($"totp_setup_{userId}");
        if (string.IsNullOrEmpty(secret))
            return string.Empty;

        var issuer = "PiedraAzul";
        var accountName = email;
        var otpUri = $"otpauth://totp/{issuer}:{accountName}?secret={secret}&issuer={issuer}";

        return otpUri;
    }

    public async Task<bool> VerifyTOTPAsync(string userId, string totp)
    {
        var mfa = await _context.UserMFAConfigurations
            .FirstOrDefaultAsync(m => m.UserId == userId && m.MFAMethod == "TOTP" && m.IsEnabled);

        if (mfa?.TOTPSecret is null)
            return false;

        var secret = Base32Encoding.ToBytes(mfa.TOTPSecret);
        var totpGenerator = new Totp(secret);

        return totpGenerator.VerifyTotp(totp, out _);
    }

    public async Task<bool> ConfirmTOTPSetupAsync(string userId, string totp)
    {
        var secret = _cache.Get<string>($"totp_setup_{userId}");
        if (string.IsNullOrEmpty(secret))
            return false;

        var secretBytes = Base32Encoding.ToBytes(secret);
        var totpGenerator = new Totp(secretBytes);

        if (!totpGenerator.VerifyTotp(totp, out _))
            return false;

        var mfa = await _context.UserMFAConfigurations
            .FirstOrDefaultAsync(m => m.UserId == userId && m.MFAMethod == "TOTP");

        if (mfa is null)
            return false;

        mfa.TOTPSecret = secret;
        mfa.IsEnabled = true;
        mfa.CreatedAt = DateTime.UtcNow;

        _cache.Remove($"totp_setup_{userId}");
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<string>> GenerateBackupCodesAsync(string userId)
    {
        var codes = new List<string>();
        var random = new Random();

        for (int i = 0; i < BackupCodeCount; i++)
        {
            var code = GenerateRandomCode(BackupCodeLength);
            codes.Add(code);
        }

        var mfa = await _context.UserMFAConfigurations
            .FirstOrDefaultAsync(m => m.UserId == userId && m.MFAMethod == "BackupCodes");

        if (mfa is null)
        {
            mfa = new UserMFAConfiguration(userId, "BackupCodes");
            await _context.UserMFAConfigurations.AddAsync(mfa);
        }

        var codesJson = JsonSerializer.Serialize(codes);
        mfa.BackupCodesEncrypted = EncryptBackupCodes(codesJson);

        await _context.SaveChangesAsync();
        return codes;
    }

    public async Task<bool> VerifyBackupCodeAsync(string userId, string code)
    {
        var mfa = await _context.UserMFAConfigurations
            .FirstOrDefaultAsync(m => m.UserId == userId);

        if (mfa?.BackupCodesEncrypted is null)
            return false;

        try
        {
            var codesJson = DecryptBackupCodes(mfa.BackupCodesEncrypted);
            var codes = JsonSerializer.Deserialize<List<string>>(codesJson) ?? new List<string>();

            if (!codes.Contains(code))
                return false;

            codes.Remove(code);
            var updatedJson = JsonSerializer.Serialize(codes);
            mfa.BackupCodesEncrypted = EncryptBackupCodes(updatedJson);

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }

    private string EncryptBackupCodes(string codes)
    {
        var key = _configuration["Security:EncryptionKey"];
        if (string.IsNullOrEmpty(key))
            throw new InvalidOperationException("Security:EncryptionKey debe estar configurada en appsettings.json");

        var keyBytes = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
        var codesBytes = Encoding.UTF8.GetBytes(codes);

        using (var aes = System.Security.Cryptography.Aes.Create())
        {
            aes.Key = keyBytes;
            aes.Mode = System.Security.Cryptography.CipherMode.CBC;
            aes.Padding = System.Security.Cryptography.PaddingMode.PKCS7;

            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            {
                using (var ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (var cs = new System.Security.Cryptography.CryptoStream(ms, encryptor, System.Security.Cryptography.CryptoStreamMode.Write))
                    {
                        cs.Write(codesBytes, 0, codesBytes.Length);
                        cs.FlushFinalBlock();
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
        }
    }

    public async Task<bool> SendOTPEmailAsync(string userId, string email)
    {
        var otp = _cache.Get<string>($"mfa_otp_{userId}");
        if (string.IsNullOrEmpty(otp))
            return false;

        return await _emailService.SendMFAEmailAsync(email, email, otp, 10);
    }

    private string DecryptBackupCodes(string encryptedCodes)
    {
        var key = _configuration["Security:EncryptionKey"];
        if (string.IsNullOrEmpty(key))
            throw new InvalidOperationException("Security:EncryptionKey debe estar configurada en appsettings.json");

        var keyBytes = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
        var encryptedBytes = Convert.FromBase64String(encryptedCodes);

        using (var aes = System.Security.Cryptography.Aes.Create())
        {
            aes.Key = keyBytes;
            aes.Mode = System.Security.Cryptography.CipherMode.CBC;
            aes.Padding = System.Security.Cryptography.PaddingMode.PKCS7;

            var iv = new byte[aes.IV.Length];
            Array.Copy(encryptedBytes, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            {
                using (var ms = new MemoryStream(encryptedBytes, iv.Length, encryptedBytes.Length - iv.Length))
                {
                    using (var cs = new System.Security.Cryptography.CryptoStream(ms, decryptor, System.Security.Cryptography.CryptoStreamMode.Read))
                    {
                        using (var reader = new StreamReader(cs))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}
