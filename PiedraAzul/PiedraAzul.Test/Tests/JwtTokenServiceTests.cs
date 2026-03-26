using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PiedraAzul.ApplicationServices.Services;
using PiedraAzul.Data;
using System.IdentityModel.Tokens.Jwt;

namespace PiedraAzul.Test.Tests
{
    public class JwtTokenServiceTests : IClassFixture<PostgresFixture>
    {
        private readonly PostgresFixture _fixture;
        private readonly IJwtTokenService _sut;

        public JwtTokenServiceTests(PostgresFixture fixture)
        {
            _fixture = fixture;
            var userManager = _fixture.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var config = _fixture.ServiceProvider.GetRequiredService<IConfiguration>();
            _sut = new JwtTokenService(userManager, config);
        }

        private async Task<ApplicationUser> SeedUserAsync(string name)
        {
            var userManager = _fixture.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = $"{name.ToLower().Replace(" ", "")}@test.com",
                Email = $"{name.ToLower().Replace(" ", "")}@test.com",
                Name = name
            };

            await userManager.CreateAsync(user, "Test123*");
            return user;
        }

        [Fact]
        public async Task CreateToken_ReturnsNonNullString()
        {
            var user = await SeedUserAsync("Token User");

            var token = await _sut.CreateTokenAsync(user);

            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public async Task GetUserIdByToken_ValidToken_ReturnsUserId()
        {
            var user = await SeedUserAsync("Validator User");

            var token = await _sut.CreateTokenAsync(user);
            var userId = await _sut.GetUserIdByToken(token);

            Assert.Equal(user.Id, userId);
        }

        [Fact]
        public async Task GetUserIdByToken_InvalidToken_ReturnsNull()
        {
            var userId = await _sut.GetUserIdByToken("not.a.valid.jwt.token");

            Assert.Null(userId);
        }
    }
}
