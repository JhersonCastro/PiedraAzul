using Hangfire.Dashboard;
using Microsoft.Extensions.Configuration;

namespace PiedraAzul.Infrastructure.BackgroundJobs;

/// <summary>
/// Permite acceso al dashboard de Hangfire si la petición incluye
/// el query param o header "X-Hangfire-Key" con el valor configurado
/// en "Hangfire:DashboardKey". Sin clave configurada el dashboard queda cerrado.
/// Uso: GET /hangfire?key=tu-clave-secreta
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly string? _dashboardKey;

    public HangfireDashboardAuthorizationFilter(IConfiguration configuration)
    {
        _dashboardKey = configuration["Hangfire:DashboardKey"];
    }

    public bool Authorize(DashboardContext context)
    {
        // Si no hay clave configurada, bloquear siempre
        if (string.IsNullOrWhiteSpace(_dashboardKey))
            return false;

        var httpContext = context.GetHttpContext();

        // Aceptar por query param: /hangfire?key=xxx
        var queryKey = httpContext.Request.Query["key"].ToString();
        if (!string.IsNullOrEmpty(queryKey) && queryKey == _dashboardKey)
            return true;

        // Aceptar por header: X-Hangfire-Key: xxx
        var headerKey = httpContext.Request.Headers["X-Hangfire-Key"].ToString();
        if (!string.IsNullOrEmpty(headerKey) && headerKey == _dashboardKey)
            return true;

        return false;
    }
}
