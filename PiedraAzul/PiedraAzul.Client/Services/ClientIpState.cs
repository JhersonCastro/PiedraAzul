namespace PiedraAzul.Client.Services;

/// <summary>
/// Guarda el IP real del cliente capturado en el prerender (navegador → proxy),
/// para reenviarlo en las llamadas GraphQL hechas desde el servidor (circuito Blazor).
/// </summary>
public class ClientIpState
{
    public string? ClientIp { get; set; }
}

/// <summary>Resuelve el IP del cliente. Implementación real solo en el servidor.</summary>
public interface IClientIpResolver
{
    string? Resolve();
}

/// <summary>Implementación por defecto (WASM): no hay HttpContext, devuelve null.</summary>
public sealed class NullClientIpResolver : IClientIpResolver
{
    public string? Resolve() => null;
}
