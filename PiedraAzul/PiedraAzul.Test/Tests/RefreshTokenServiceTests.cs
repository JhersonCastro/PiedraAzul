using PiedraAzul.ApplicationServices.Services;
using PiedraAzul.Data;
using PiedraAzul.Data.Models;

namespace PiedraAzul.Test.Tests
{
    public class RefreshTokenServiceTests : IClassFixture<PostgresFixture>
    {
        private readonly PostgresFixture _fixture;
        private readonly RefreshTokenService _sut;

        public RefreshTokenServiceTests(PostgresFixture fixture)
        {
            _fixture = fixture;
            _sut = new RefreshTokenService(fixture.DbContextFactory);
        }

        private async Task<ApplicationUser> SeedUserAsync(string name)
        {
            await using var ctx = _fixture.DbContextFactory.CreateDbContext();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = $"{name.ToLower().Replace(" ", "")}@test.com",
                Name = name
            };

            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            return user;
        }

        [Fact]
        public async Task GenerateRefreshToken_ReturnsTokenAndSaves()
        {
            var user = await SeedUserAsync("Refresh User");

            var tokenValue = await _sut.GenerateRefreshTokenAsync(user.Id);

            Assert.NotNull(tokenValue);
            
            await using var ctx = _fixture.DbContextFactory.CreateDbContext();
            var exists = ctx.RefreshTokens.Any(rt => rt.Token == tokenValue && rt.UserId == user.Id);
            Assert.True(exists);
        }

        [Fact]
        public async Task ValidateRefreshToken_Valid_ReturnsToken()
        {
            var user = await SeedUserAsync("Valid Token User");
            var tokenValue = await _sut.GenerateRefreshTokenAsync(user.Id);

            var result = await _sut.ValidateRefreshTokenAsync(tokenValue);

            Assert.NotNull(result);
            Assert.Equal(user.Id, result!.UserId);
        }

        [Fact]
        public async Task ValidateRefreshToken_Revoked_ReturnsNull()
        {
            var user = await SeedUserAsync("Revoked Token User");
            
            var rtValue = "revoked-token-123";
            await using (var ctx = _fixture.DbContextFactory.CreateDbContext())
            {
                ctx.RefreshTokens.Add(new RefreshToken
                {
                    Token = rtValue,
                    UserId = user.Id,
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    isRevoked = true
                });
                await ctx.SaveChangesAsync();
            }

            var result = await _sut.ValidateRefreshTokenAsync(rtValue);

            Assert.Null(result);
        }

        [Fact]
        public async Task RotateRefreshToken_Success()
        {
            var user = await SeedUserAsync("Rotate User");
            var oldTokenValue = await _sut.GenerateRefreshTokenAsync(user.Id);
            
            var oldToken = await _sut.ValidateRefreshTokenAsync(oldTokenValue);
            var newTokenValue = await _sut.RotateRefreshTokenAsync(oldToken!);

            Assert.NotEqual(oldTokenValue, newTokenValue);
            
            await using var ctx = _fixture.DbContextFactory.CreateDbContext();
            var revoked = ctx.RefreshTokens.First(rt => rt.Token == oldTokenValue);
            Assert.True(revoked.isRevoked);
        }
    }
}
