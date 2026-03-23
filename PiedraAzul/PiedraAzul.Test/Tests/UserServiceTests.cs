using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PiedraAzul.ApplicationServices.Services;
using PiedraAzul.Data;
using PiedraAzul.Data.Models;

namespace PiedraAzul.Test.Tests
{
    public class UserServiceTests : IClassFixture<PostgresFixture>
    {
        private readonly PostgresFixture _fixture;
        private readonly IUserService _sut;

        public UserServiceTests(PostgresFixture fixture)
        {
            _fixture = fixture;

            var userManager = _fixture.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = _fixture.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            _sut = new UserService(_fixture.DbContextFactory, userManager, roleManager);
        }

        // ─────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────

        private async Task<ApplicationUser> SeedUserAsync()
        {
            await using var ctx = _fixture.DbContextFactory.CreateDbContext();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = $"{Guid.NewGuid()}@test.com",
                PhoneNumber = Guid.NewGuid().ToString(),
                IdentificationNumber = Guid.NewGuid().ToString(),
                UserName = Guid.NewGuid().ToString(),
                Name = "Test User",
                PasswordHash = new PasswordHasher<ApplicationUser>()
                    .HashPassword(null!, "Test123*")
            };

            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            return user;
        }

        private async Task SeedRoleAsync(string role)
        {
            var roleManager = _fixture.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));

                if (!result.Succeeded)
                {
                    throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        private async Task AddUserToRoleAsync(ApplicationUser user, string role)
        {
            await using var ctx = _fixture.DbContextFactory.CreateDbContext();

            var roleEntity = await ctx.Roles.FirstAsync(r => r.Name == role);

            ctx.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = user.Id,
                RoleId = roleEntity.Id
            });

            await ctx.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────
        // TESTS
        // ─────────────────────────────────────────────

        [Fact]
        public async Task Register_WithValidRoles_Success()
        {
            await SeedRoleAsync("patient");

            var user = new ApplicationUser
            {
                Email = $"{Guid.NewGuid()}@test.com",
                PhoneNumber = Guid.NewGuid().ToString(),
                IdentificationNumber = Guid.NewGuid().ToString(),
                Name = "New User"
            };

            var result = await _sut.Register(user, "Test123*", new List<string> { "patient" });

            Assert.NotNull(result);
        }

        [Fact]
        public async Task Register_InvalidRole_ReturnsNull()
        {
            var user = new ApplicationUser
            {
                Email = $"{Guid.NewGuid()}@test.com",
                PhoneNumber = Guid.NewGuid().ToString(),
                IdentificationNumber = Guid.NewGuid().ToString(),
                Name = "New User"
            };

            var result = await _sut.Register(user, "Test123*", new List<string> { "invalid-role" });

            Assert.Null(result);
        }

        [Fact]
        public async Task Login_WithEmail_Success()
        {
            await SeedRoleAsync("patient");

            var user = await SeedUserAsync();
            await AddUserToRoleAsync(user, "patient");

            var (resultUser, roles) = await _sut.Login(user.Email, "Test123*");

            Assert.NotNull(resultUser);
            Assert.Contains("patient", roles);
        }

        [Fact]
        public async Task Login_InvalidPassword_ReturnsNull()
        {
            var user = await SeedUserAsync();

            var (resultUser, roles) = await _sut.Login(user.Email, "wrong-password");

            Assert.Null(resultUser);
            Assert.Null(roles);
        }

        [Fact]
        public async Task Login_UserNotFound_ReturnsNull()
        {
            var (user, roles) = await _sut.Login("no-existe@test.com", "1234");

            Assert.Null(user);
            Assert.Null(roles);
        }

        [Fact]
        public async Task GetById_ReturnsUser()
        {
            var user = await SeedUserAsync();

            var result = await _sut.GetById(user.Id);

            Assert.NotNull(result);
            Assert.Equal(user.Id, result!.Id);
        }

        [Fact]
        public async Task GetById_NotFound_ReturnsNull()
        {
            var result = await _sut.GetById(Guid.NewGuid().ToString());

            Assert.Null(result);
        }

        [Fact]
        public async Task GetRolesByUser_ReturnsRoles()
        {
            await SeedRoleAsync("doctor");

            var user = await SeedUserAsync();
            await AddUserToRoleAsync(user, "doctor");

            var roles = await _sut.GetRolesByUser(user);

            Assert.Contains("doctor", roles);
        }

        [Fact]
        public async Task GetRolesByUser_NoRoles_ReturnsEmpty()
        {
            var user = await SeedUserAsync();

            var roles = await _sut.GetRolesByUser(user);

            Assert.Empty(roles);
        }

        [Fact]
        public async Task CreateProfileForRole_Patient_CreatesProfile()
        {
            var user = await SeedUserAsync();

            await _sut.CreateProfileForRoleAsync(user, "patient");

            await using var ctx = _fixture.DbContextFactory.CreateDbContext();

            var exists = await ctx.PatientProfiles.AnyAsync(p => p.UserId == user.Id);

            Assert.True(exists);
        }

        [Fact]
        public async Task CreateProfileForRole_Doctor_CreatesProfile()
        {
            var user = await SeedUserAsync();

            await _sut.CreateProfileForRoleAsync(user, "doctor");

            await using var ctx = _fixture.DbContextFactory.CreateDbContext();

            var exists = await ctx.DoctorProfiles.AnyAsync(d => d.UserId == user.Id);

            Assert.True(exists);
        }

        [Fact]
        public async Task CreateProfileForRole_Duplicate_DoesNotCreateTwice()
        {
            var user = await SeedUserAsync();

            await _sut.CreateProfileForRoleAsync(user, "patient");
            await _sut.CreateProfileForRoleAsync(user, "patient");

            await using var ctx = _fixture.DbContextFactory.CreateDbContext();

            var count = await ctx.PatientProfiles.CountAsync(p => p.UserId == user.Id);

            Assert.Equal(1, count);
        }

        [Fact]
        public async Task CreateProfileForRole_InvalidRole_Throws()
        {
            var user = await SeedUserAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.CreateProfileForRoleAsync(user, "admin"));
        }
    }
}