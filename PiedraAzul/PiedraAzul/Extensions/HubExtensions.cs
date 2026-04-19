using Microsoft.AspNetCore.Authorization;
using PiedraAzul.RealTime.Hubs;
using System.Security.Claims;
using IOPath = System.IO.Path;

namespace PiedraAzul.Extensions;

public static class HubExtensions
{
    public static WebApplication MapHubs(this WebApplication app)
    {
        app.MapHub<AppointmentHub>("/hubs/appointments");
        return app;
    }

    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        app.MapPost("/api/avatar", [Authorize] async (
            IFormFile file,
            HttpContext ctx,
            IWebHostEnvironment env,
            ILogger<Program> logger) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Results.Unauthorized();

            if (file.Length > 5 * 1024 * 1024)
                return Results.BadRequest("El archivo no puede superar 5 MB.");

            var ext = IOPath.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExts = new HashSet<string> { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExts.Contains(ext))
                return Results.BadRequest("Formato no permitido. Usa JPG, PNG o WebP.");

            try
            {
                // Ensure we use WebRootPath - it should always be set for static file serving
                if (string.IsNullOrEmpty(env.WebRootPath))
                {
                    var errorMsg = $"WebRootPath is null or empty. ContentRootPath: {env.ContentRootPath}";
                    logger.LogError(errorMsg);
                    return Results.Json(
                        new { error = errorMsg },
                        statusCode: 500);
                }

                var avatarsPath = IOPath.Combine(env.WebRootPath, "Avatars");
                Directory.CreateDirectory(avatarsPath);

                logger.LogInformation($"Avatar directory path: {avatarsPath}");

                var fileName = $"{userId}{ext}";
                var filePath = IOPath.Combine(avatarsPath, fileName);

                // Delete old avatar if exists
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    logger.LogInformation($"Deleted old avatar: {filePath}");
                }

                await using var stream = File.Create(filePath);
                await file.CopyToAsync(stream);
                logger.LogInformation($"Avatar saved successfully: {filePath}");

                var url = $"/Avatars/{fileName}?t={DateTimeOffset.Now.ToUnixTimeSeconds()}";
                logger.LogInformation($"Returning avatar URL: {url}");

                return Results.Ok(new { url = url });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error saving avatar for user {userId}: {ex.Message}");
                return Results.Json(
                    new { error = $"Error guardando avatar: {ex.Message}" },
                    statusCode: 500);
            }
        }).DisableAntiforgery();

        return app;
    }
}
