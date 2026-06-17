using System.Text.Json;

namespace PiedraAzul.Middleware;

public class AuthRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthRateLimitMiddleware> _logger;
    private static readonly Dictionary<string, (int Count, DateTime ResetTime)> _attempts = new();
    private static readonly object _lockObj = new();
    private const int MaxAttempts = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public AuthRateLimitMiddleware(RequestDelegate next, ILogger<AuthRateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path == "/graphql" && context.Request.Method == "POST")
        {
            var body = await ReadBodyAsync(context.Request);
            _logger.LogInformation("GraphQL Request Body: {Body}", body[..Math.Min(200, body.Length)]);

            if (IsAuthMutation(body))
            {
                var clientId = GetClientId(context);
                _logger.LogInformation("Auth mutation detected for client: {ClientId}", clientId);

                if (!CheckRateLimit(clientId))
                {
                    _logger.LogWarning("Rate limit exceeded for auth mutation from: {ClientId}", clientId);
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.Response.WriteAsync("Too many auth attempts. Please try again later.");
                    return;
                }

                _logger.LogInformation("Rate limit check passed for: {ClientId}", clientId);
            }
            else
            {
                _logger.LogInformation("Not an auth mutation");
            }
        }

        await _next(context);
    }

    private static async Task<string> ReadBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();
        var body = await new StreamReader(request.Body).ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }

    private static bool IsAuthMutation(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);

            // Buscar en operationName primero
            var operationName = doc.RootElement
                .TryGetProperty("operationName", out var op)
                ? op.GetString()
                : null;

            if (operationName?.Contains("Login", StringComparison.OrdinalIgnoreCase) == true ||
                operationName?.Contains("Register", StringComparison.OrdinalIgnoreCase) == true)
                return true;

            // Si no hay operationName, buscar en el query
            var query = doc.RootElement
                .TryGetProperty("query", out var q)
                ? q.GetString()
                : null;

            return query?.Contains("mutation Login", StringComparison.OrdinalIgnoreCase) == true ||
                   query?.Contains("mutation Register", StringComparison.OrdinalIgnoreCase) == true;
        }
        catch
        {
            return false;
        }
    }

    private static string GetClientId(HttpContext context)
    {
        return context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";
    }

    private bool CheckRateLimit(string clientId)
    {
        lock (_lockObj)
        {
            var now = DateTime.UtcNow;

            if (_attempts.TryGetValue(clientId, out var attempt))
            {
                var (count, resetTime) = attempt;
                _logger.LogInformation("Current attempts for {ClientId}: {Count}/{Max}, ResetTime: {ResetTime}",
                    clientId, count, MaxAttempts, resetTime);

                if (now > resetTime)
                {
                    _logger.LogInformation("Window expired, resetting for {ClientId}", clientId);
                    _attempts[clientId] = (1, now.Add(Window));
                    return true;
                }

                if (count >= MaxAttempts)
                {
                    _logger.LogWarning("Max attempts reached for {ClientId}", clientId);
                    return false;
                }

                _attempts[clientId] = (count + 1, resetTime);
                _logger.LogInformation("Attempt incremented for {ClientId}: {NewCount}/{Max}",
                    clientId, count + 1, MaxAttempts);
                return true;
            }

            _logger.LogInformation("First attempt for {ClientId}", clientId);
            _attempts[clientId] = (1, now.Add(Window));
            return true;
        }
    }
}
