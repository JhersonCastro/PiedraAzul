using Grpc.Core;

namespace PiedraAzul.GrpcServices
{
    public static class GrpcCookieHelper
    {
        public static async Task SetRefreshTokenCookie(ServerCallContext context, string refreshToken, bool isProduction = true)
        {
            var cookie = $"refreshToken={refreshToken}; " +
                         $"HttpOnly; " +
                         $"Path=/; " +
                         $"SameSite=Strict; " +
                         $"{(isProduction ? "Secure;" : "")} " +
                         $"Max-Age={7 * 24 * 60 * 60}";

            await context.WriteResponseHeadersAsync(new Metadata
        {
            { "Set-Cookie", cookie }
        });
        }

        public static string? GetRefreshTokenFromCookie(ServerCallContext context)
        {
            var cookieHeader = context.RequestHeaders
                .FirstOrDefault(h => h.Key == "cookie")?.Value;

            if (string.IsNullOrEmpty(cookieHeader))
                return null;

            return cookieHeader
                .Split(';')
                .Select(c => c.Trim())
                .FirstOrDefault(c => c.StartsWith("refreshToken="))
                ?.Split('=')[1];
        }

        public static async Task DeleteRefreshTokenCookie(ServerCallContext context)
        {
            var cookie = "refreshToken=; HttpOnly; Path=/; Max-Age=0; SameSite=Strict";

            await context.WriteResponseHeadersAsync(new Metadata
        {
            { "Set-Cookie", cookie }
        });
        }
    }
}
