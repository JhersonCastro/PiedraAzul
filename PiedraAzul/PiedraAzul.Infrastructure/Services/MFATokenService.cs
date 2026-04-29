using Microsoft.Extensions.Caching.Memory;
using PiedraAzul.Application.Common.Interfaces;

namespace PiedraAzul.Infrastructure.Services;

public class MFATokenService : IMFATokenService
{
    private readonly IMemoryCache _cache;

    public MFATokenService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string GenerateMFAToken(string userId)
    {
        var token = $"mfa_{userId}_{Guid.NewGuid()}";
        _cache.Set(token, userId, TimeSpan.FromMinutes(5));
        return token;
    }

    public string? ValidateMFAToken(string token)
    {
        if (_cache.TryGetValue(token, out string? userId))
        {
            _cache.Remove(token);
            return userId;
        }

        return null;
    }
}
