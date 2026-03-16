using Microsoft.EntityFrameworkCore;
using PiedraAzul.Data;
using PiedraAzul.Data.Models;
using System.Security.Cryptography;

namespace PiedraAzul.ApplicationServices.Services
{
    public interface IRefreshTokenService
    {
        Task<string> GenerateRefreshTokenAsync(string userId);
        Task<RefreshToken?> ValidateRefreshTokenAsync(string token);
        Task<string> RotateRefreshTokenAsync(RefreshToken token);
    }
    public class RefreshTokenService(IDbContextFactory<AppDbContext> dbContext) : IRefreshTokenService
    {
        public async Task<string> GenerateRefreshTokenAsync(string userId)
        {
            using var context = await dbContext.CreateDbContextAsync();

            var token = GenerateRefreshToken();

            var refreshToken = new RefreshToken
            {
                Token = token,
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                isRevoked = false
            };

            await context.RefreshTokens.AddAsync(refreshToken);
            await context.SaveChangesAsync();
            Console.WriteLine("Token guardado " + refreshToken.Token + " Expira" + refreshToken.ExpiresAt.ToShortDateString());
            return token;
        }

        public async Task<string> RotateRefreshTokenAsync(RefreshToken oldToken)
        {
            using var context = await dbContext.CreateDbContextAsync();

            // volver a adjuntar la entidad
            context.RefreshTokens.Attach(oldToken);
            oldToken.isRevoked = true;

            var newTokenValue = GenerateRefreshToken();

            context.RefreshTokens.Add(new RefreshToken
            {
                Token = newTokenValue,
                UserId = oldToken.UserId,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                isRevoked = false
            });

            await context.SaveChangesAsync();
            return newTokenValue;
        }

        public async Task<RefreshToken?> ValidateRefreshTokenAsync(string token)
        {
            using var context = await dbContext.CreateDbContextAsync();
            var refresToken = await context.RefreshTokens
                .Include(x => x.User)
                .Where(rt => rt.Token == token && !rt.isRevoked && rt.ExpiresAt > DateTime.UtcNow)
                .SingleOrDefaultAsync();
            return refresToken;
        }
        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
