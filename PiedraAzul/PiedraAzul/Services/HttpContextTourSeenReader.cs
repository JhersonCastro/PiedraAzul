using Microsoft.AspNetCore.Http;
using PiedraAzul.Client.Services;

namespace PiedraAzul.Services;

/// <summary>
/// Implementación de servidor: lee la cookie "tourSeen_{tourId}" del request HTTP durante el prerender SSR.
/// </summary>
public sealed class HttpContextTourSeenReader(IHttpContextAccessor accessor) : ITourSeenReader
{
    public bool IsSeen(string tourId) =>
        accessor.HttpContext?.Request.Cookies.ContainsKey("tourSeen_" + tourId) == true;
}
