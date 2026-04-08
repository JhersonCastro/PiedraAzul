using Microsoft.EntityFrameworkCore;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Infrastructure.Auth;
using PiedraAzul.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;

namespace PiedraAzul.Infrastructure.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly AppDbContext _context;

        public RefreshTokenService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateRefreshTokenAsync(string userId)
        {
            var rawToken = GenerateToken();
            var hashed = HashToken(rawToken);

            var refreshToken = new RefreshToken(
                hashed,
                DateTime.UtcNow.AddDays(7),
                userId
            );

            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            return rawToken;
        }

        public async Task<string?> ValidateRefreshTokenAsync(string token)
        {
            var hashed = HashToken(token);

            var stored = await _context.RefreshTokens
                .Where(x =>
                    x.TokenHashed == hashed &&
                    !x.IsRevoked &&
                    x.ExpiresAt > DateTime.UtcNow)
                .SingleOrDefaultAsync();

            return stored?.UserId;
        }

        public async Task<string> RotateRefreshTokenAsync(string token)
        {
            var hashed = HashToken(token);

            var stored = await _context.RefreshTokens
                .SingleOrDefaultAsync(x => x.TokenHashed == hashed);

            if (stored == null)
                throw new Exception("Invalid refresh token");

            // 🔥 Revoca el actual
            stored.Revoke();

            var newRaw = GenerateToken();

            var newToken = new RefreshToken(
                HashToken(newRaw),
                DateTime.UtcNow.AddDays(7),
                stored.UserId
            );

            await _context.RefreshTokens.AddAsync(newToken);
            await _context.SaveChangesAsync();

            return newRaw;
        }

        // =========================
        // HELPERS
        // =========================

        private static string GenerateToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }
    }
}