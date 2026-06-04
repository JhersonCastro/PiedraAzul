using System.Linq;
using Microsoft.AspNetCore.Http;
using PiedraAzul.Client.Services;

namespace PiedraAzul.Services;

/// <summary>
/// Implementación de servidor: obtiene el IP real del cliente desde el HttpContext del
/// prerender (respeta X-Forwarded-For del proxy y normaliza loopback IPv6).
/// </summary>
public sealed class HttpContextClientIpResolver(IHttpContextAccessor accessor) : IClientIpResolver
{
    public string? Resolve()
    {
        var ctx = accessor.HttpContext;
        if (ctx is null) return null;

        var forwarded = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();

        var addr = ctx.Connection.RemoteIpAddress;
        if (addr is null) return null;

        if (addr.IsIPv4MappedToIPv6)
            addr = addr.MapToIPv4();

        var s = addr.ToString();
        return s == "::1" ? "127.0.0.1" : s;
    }
}
