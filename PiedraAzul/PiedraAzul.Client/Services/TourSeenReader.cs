namespace PiedraAzul.Client.Services;

/// <summary>Verifica si un tour ya fue visto leyendo la cookie del request. Solo disponible server-side.</summary>
public interface ITourSeenReader
{
    bool IsSeen(string tourId);
}

/// <summary>Implementación WASM: sin HttpContext, siempre devuelve false — localStorage lo verifica en AfterRender.</summary>
public sealed class NullTourSeenReader : ITourSeenReader
{
    public bool IsSeen(string tourId) => false;
}
