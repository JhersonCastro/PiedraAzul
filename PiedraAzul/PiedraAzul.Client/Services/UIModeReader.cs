using PiedraAzul.Client.States;

namespace PiedraAzul.Client.Services;

/// <summary>Resuelve el modo UI preferido desde el contexto de la petición. Solo disponible server-side.</summary>
public interface IUIModeReader
{
    UIMode? Read();
}

/// <summary>Implementación WASM: sin HttpContext, devuelve null — localStorage lo maneja en AfterRender.</summary>
public sealed class NullUIModeReader : IUIModeReader
{
    public UIMode? Read() => null;
}
