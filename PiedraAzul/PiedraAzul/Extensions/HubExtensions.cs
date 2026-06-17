using Microsoft.AspNetCore.Authorization;
using PiedraAzul.RealTime.Hubs;
using System.Security.Claims;
using System.Text.Json;
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

                // Use unique filename with UserId + Guid to identify user's avatars
                var fileName = $"{userId}-{Guid.NewGuid()}{ext}";
                var filePath = IOPath.Combine(avatarsPath, fileName);

                // Delete old avatars for THIS user only (keep only latest 3)
                var userAvatarPattern = $"{userId}-*{ext}";
                var userAvatars = Directory.GetFiles(avatarsPath, userAvatarPattern)
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .Skip(3)
                    .ToList();

                foreach (var oldFile in userAvatars)
                {
                    try { File.Delete(oldFile); }
                    catch { }
                }

                if (userAvatars.Count > 0)
                    logger.LogInformation($"Cleaned up {userAvatars.Count} old avatars for user {userId}");

                var fileSize = file.Length;
                logger.LogWarning($"[AVATAR] Receiving file: {file.FileName}, size: {fileSize} bytes");

                // Write file in chunks to ensure complete transfer
                const int bufferSize = 64 * 1024;
                long bytesWritten = 0;

                await using var fileStream = file.OpenReadStream();
                await using var diskStream = File.Create(filePath);
                var buffer = new byte[bufferSize];
                int bytesRead;

                while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await diskStream.WriteAsync(buffer, 0, bytesRead);
                    bytesWritten += bytesRead;
                }

                logger.LogWarning($"[AVATAR] File written: {filePath}, wrote {bytesWritten} bytes, expected {fileSize} bytes");

                if (bytesWritten != fileSize)
                {
                    File.Delete(filePath);
                    logger.LogError($"[AVATAR] INCOMPLETE: got {bytesWritten} of {fileSize} bytes for {file.FileName}");
                    return Results.BadRequest($"Error: transferencia incompleta ({bytesWritten} de {fileSize} bytes). Por favor intenta de nuevo.");
                }

                logger.LogInformation($"[AVATAR] Success: {filePath} ({bytesWritten} bytes)");

                var url = $"/Avatars/{fileName}";
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

    public static WebApplication MapWhatsAppWebhook(this WebApplication app)
    {
        // ── Verificación del webhook (Meta llama a esto al registrar) ──────────
        app.MapGet("/api/whatsapp/webhook", (
            HttpContext ctx,
            IConfiguration config) =>
        {
            var mode      = ctx.Request.Query["hub.mode"].ToString();
            var token     = ctx.Request.Query["hub.verify_token"].ToString();
            var challenge = ctx.Request.Query["hub.challenge"].ToString();

            var expectedToken = config["WhatsApp:WebhookVerifyToken"] ?? "";

            if (mode == "subscribe" && token == expectedToken)
                return Results.Text(challenge); // Meta espera el challenge como texto plano (sin JSON)

            return Results.Unauthorized();
        });

        // ── Recepción de eventos de Meta (entregas, lecturas, mensajes entrantes) ──
        app.MapPost("/api/whatsapp/webhook", async (
            HttpContext ctx,
            ILogger<Program> logger) =>
        {
            using var reader = new StreamReader(ctx.Request.Body);
            var body = await reader.ReadToEndAsync();

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                // Recorre las entradas del webhook
                if (root.TryGetProperty("entry", out var entries))
                {
                    foreach (var entry in entries.EnumerateArray())
                    {
                        if (!entry.TryGetProperty("changes", out var changes)) continue;
                        foreach (var change in changes.EnumerateArray())
                        {
                            if (!change.TryGetProperty("value", out var value)) continue;

                            // Mensajes entrantes
                            if (value.TryGetProperty("messages", out var messages))
                            {
                                foreach (var msg in messages.EnumerateArray())
                                {
                                    var from = msg.TryGetProperty("from", out var f) ? f.GetString() : "?";
                                    var type = msg.TryGetProperty("type", out var t) ? t.GetString() : "?";
                                    logger.LogInformation("[WhatsApp] Mensaje de {From}, tipo: {Type}", from, type);
                                }
                            }

                            // Actualizaciones de estado (sent, delivered, read, failed)
                            if (value.TryGetProperty("statuses", out var statuses))
                            {
                                foreach (var status in statuses.EnumerateArray())
                                {
                                    var id     = status.TryGetProperty("id",     out var i) ? i.GetString() : "?";
                                    var st     = status.TryGetProperty("status", out var s) ? s.GetString() : "?";
                                    var recip  = status.TryGetProperty("recipient_id", out var r) ? r.GetString() : "?";
                                    logger.LogInformation("[WhatsApp] Mensaje {Id} → {Status} ({Recipient})", id, st, recip);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[WhatsApp] Error procesando webhook");
            }

            return Results.Ok(); // Siempre 200 a Meta
        }).DisableAntiforgery();

        return app;
    }
}
