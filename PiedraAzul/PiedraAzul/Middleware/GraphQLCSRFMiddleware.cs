namespace PiedraAzul.Middleware;

public class GraphQLCSRFMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GraphQLCSRFMiddleware> _logger;

    public GraphQLCSRFMiddleware(RequestDelegate next, ILogger<GraphQLCSRFMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path == "/graphql" && context.Request.Method == "POST")
        {
            var referer = context.Request.Headers["Referer"].ToString();
            var origin = context.Request.Headers["Origin"].ToString();
            var host = context.Request.Host.ToString();
            var hasAuthCookie = context.Request.Cookies.ContainsKey(".AspNetCore.Identity.Application");

            _logger.LogInformation("GraphQL CSRF Check - Origin: {Origin}, Referer: {Referer}, Host: {Host}, AuthCookie: {HasCookie}",
                string.IsNullOrEmpty(origin) ? "(empty)" : origin,
                string.IsNullOrEmpty(referer) ? "(empty)" : referer,
                host,
                hasAuthCookie);

            // Solo validar si hay Origin o Referer presentes
            if (!string.IsNullOrEmpty(origin) && !origin.Contains(host, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("CSRF: Origin mismatch. Expected: {Host}, Got: {Origin}", host, origin);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("CSRF validation failed");
                return;
            }

            if (!string.IsNullOrEmpty(referer) && !referer.Contains(host, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("CSRF: Referer mismatch. Expected: {Host}, Got: {Referer}", host, referer);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("CSRF validation failed");
                return;
            }
        }

        await _next(context);
    }
}
