using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
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
        var mfa = await _context.UserMFAConfigurations
            .FirstOrDefaultAsync(m => m.UserId == userId);

        return mfa?.IsEnabled ?? false;
    }

    public async Task<string> GetMFAMethodAsync(string userId)
    {
        var mfa = await _context.UserMFAConfigurations
            .FirstOrDefaultAsync(m => m.UserId == userId);

        return mfa?.MFAMethod ?? "Email";
    }

    public async Task<bool> EnableMFAAsync(string userId, string method)
    {
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
        return true;
    }

    public async Task<bool> DisableMFAAsync(string userId)
    {
        var mfa = await _context.UserMFAConfigurations
            .FirstOrDefaultAsync(m => m.UserId == userId);

        if (mfa is null)
            return false;

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
        // Generate 6-digit OTP
        var otp = new Random().Next(100000, 999999).ToString();

        // Store in cache with 10-minute expiration
        var expirationMinutes = _configuration.GetValue<int>("Security:MFA:OTPExpirationMinutes", 10);
        _cache.Set($"mfa_otp_{userId}", otp, TimeSpan.FromMinutes(expirationMinutes));

        return otp;
    }
}
