using Microsoft.AspNetCore.Http;
using PiedraAzul.Client.Services;
using PiedraAzul.Client.States;

namespace PiedraAzul.Services;

/// <summary>
/// Implementación de servidor: lee la cookie "uiMode" de la petición HTTP durante el prerender SSR.
/// </summary>
public sealed class HttpContextUIModeReader(IHttpContextAccessor accessor) : IUIModeReader
{
    public UIMode? Read()
    {
        var cookie = accessor.HttpContext?.Request.Cookies["uiMode"];
        if (cookie is not null && Enum.TryParse<UIMode>(cookie, ignoreCase: true, out var mode))
            return mode;
        return null;
    }
}
