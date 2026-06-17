namespace PiedraAzul.Extensions;

internal sealed class CookieForwardingHandler(IHttpContextAccessor accessor)
    : DelegatingHandler(new HttpClientHandler())
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var cookie = accessor.HttpContext?.Request.Headers.Cookie.ToString();
        if (!string.IsNullOrEmpty(cookie))
            request.Headers.TryAddWithoutValidation("Cookie", cookie);

        return base.SendAsync(request, cancellationToken);
    }
}
