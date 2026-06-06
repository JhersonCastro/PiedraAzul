using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace PiedraAzul.Infrastructure.BackgroundJobs;

/// <summary>
/// Permite acceso al dashboard de Hangfire mediante clave secreta.
/// La primera request debe incluir ?key=valor (configurado en Hangfire:DashboardKey).
/// Al validar, se establece una cookie de sesión para que los assets (CSS/JS)
/// y la navegación interna funcionen sin repetir la clave en cada URL.
/// Uso: GET /hangfire?key=tu-clave-secreta
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private const string CookieName = "hf_session";
    private readonly string? _dashboardKey;

    public HangfireDashboardAuthorizationFilter(IConfiguration configuration)
    {
        _dashboardKey = configuration["Hangfire:DashboardKey"];
    }

    public bool Authorize(DashboardContext context)
    {
        if (string.IsNullOrWhiteSpace(_dashboardKey))
            return false;

        var httpContext = context.GetHttpContext();

        // 1. Cookie de sesión ya establecida → acceso directo
        var cookie = httpContext.Request.Cookies[CookieName];
        if (cookie == _dashboardKey)
            return true;

        // 2. Validar por query param: /hangfire?key=xxx
        var queryKey = httpContext.Request.Query["key"].ToString();
        if (string.IsNullOrEmpty(queryKey))
        {
            // 3. Validar por header: X-Hangfire-Key: xxx
            queryKey = httpContext.Request.Headers["X-Hangfire-Key"].ToString();
        }

        if (!string.IsNullOrEmpty(queryKey) && queryKey == _dashboardKey)
        {
            // Establecer cookie de sesión (8 horas, HttpOnly, Secure)
            httpContext.Response.Cookies.Append(CookieName, _dashboardKey, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            });
            return true;
        }

        return false;
    }
}
