using System.Linq;
using PiedraAzul.Client.Services;

namespace PiedraAzul.Extensions;

/// <summary>
/// Para las llamadas GraphQL hechas desde el servidor (SSR / circuito Blazor) reenvía:
/// - la cookie de autenticación del request entrante, y
/// - el IP real del cliente (capturado en el prerender) vía header X-Client-Ip,
///   ya que en el circuito el HttpContext es null y la llamada saldría con el IP del servidor.
/// </summary>
internal sealed class CookieForwardingHandler(IHttpContextAccessor accessor, ClientIpState clientIp)
    : DelegatingHandler(new HttpClientHandler())
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var cookie = accessor.HttpContext?.Request.Headers.Cookie.ToString();
        if (!string.IsNullOrEmpty(cookie))
            request.Headers.TryAddWithoutValidation("Cookie", cookie);

        var ip = clientIp.ClientIp ?? ResolveFromHttpContext();
        if (!string.IsNullOrEmpty(ip))
            request.Headers.TryAddWithoutValidation("X-Client-Ip", ip);

        return base.SendAsync(request, cancellationToken);
    }

    private string? ResolveFromHttpContext()
    {
        var ctx = accessor.HttpContext;
        if (ctx is null) return null;

        var forwarded = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();

        var addr = ctx.Connection.RemoteIpAddress;
        if (addr is null) return null;
        if (addr.IsIPv4MappedToIPv6) addr = addr.MapToIPv4();
        var s = addr.ToString();
        return s == "::1" ? "127.0.0.1" : s;
    }
}
