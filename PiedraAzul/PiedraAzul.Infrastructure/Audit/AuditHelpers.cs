using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace PiedraAzul.Infrastructure.Audit;

/// <summary>Resuelve el actor (usuario) de la petición actual para la auditoría.</summary>
internal static class AuditActor
{
    public static (string? UserId, string? Name, string? Roles, string? Ip) Resolve(IHttpContextAccessor accessor)
    {
        var ctx = accessor.HttpContext;
        var ip = ctx?.Connection?.RemoteIpAddress?.ToString();

        var user = ctx?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return (null, null, null, ip);

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = user.FindFirstValue(ClaimTypes.Name) ?? user.Identity?.Name;
        var roles = string.Join(",", user.FindAll(ClaimTypes.Role).Select(c => c.Value));
        return (userId, name, string.IsNullOrEmpty(roles) ? null : roles, ip);
    }
}

/// <summary>Redacta y serializa datos de auditoría, ocultando información sensible.</summary>
internal static class AuditData
{
    private static readonly string[] SensitiveFragments =
    {
        "password", "passwordhash", "securitystamp", "concurrencystamp",
        "token", "secret", "otpcode", "otp", "backupcode", "privatekey", "credentialprivate"
    };

    public static bool IsSensitive(string propertyName)
    {
        var lower = propertyName.ToLowerInvariant();
        return SensitiveFragments.Any(lower.Contains);
    }

    public static string? Serialize(object? data)
    {
        if (data is null) return null;
        try
        {
            return JsonSerializer.Serialize(data, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };
}
