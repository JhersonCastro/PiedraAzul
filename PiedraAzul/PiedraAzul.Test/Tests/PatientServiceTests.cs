using Microsoft.EntityFrameworkCore;
using PiedraAzul.ApplicationServices.Services;
using PiedraAzul.Data;
using PiedraAzul.Data.Models;

namespace PiedraAzul.Test.Tests
{
    public class PatientServiceTests : IClassFixture<PostgresFixture>
    {
        private readonly PostgresFixture _fixture;
        private readonly PatientService _sut;

        public PatientServiceTests(PostgresFixture fixture)
        {
            _fixture = fixture;
            _sut = new PatientService(fixture.DbContextFactory);
        }

        private async Task<PatientGuest> SeedGuestAsync(string name, string id)
        {
            await using var ctx = _fixture.DbContextFactory.CreateDbContext();

            var guest = new PatientGuest
            {
                PatientIdentification = id,
                PatientName = name,
                PatientPhone = "3001234567",
                PatientExtraInfo = "Test Info"
            };

            ctx.PatientGuests.Add(guest);
            await ctx.SaveChangesAsync();

            return guest;
        }

        private async Task<ApplicationUser> SeedPatientUserAsync(string name, string email)
        {
            await using var ctx = _fixture.DbContextFactory.CreateDbContext();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = $"{name.ToLower().Replace(" ", "")}@test.com",
                Name = name,
                Email = email,
                PhoneNumber = "555-0000"
            };

            var profile = new PatientProfile
            {
                UserId = user.Id,
                User = user
            };

            ctx.Users.Add(user);
            ctx.PatientProfiles.Add(profile);
            await ctx.SaveChangesAsync();

            return user;
        }

        [Fact]
        public async Task CreatePatientGuest_Success()
        {
            var guest = new PatientGuest
            {
                PatientIdentification = "87654321",
                PatientName = "New Guest",
                PatientPhone = "3100000000",
                PatientExtraInfo = "New Info"
            };

            var result = await _sut.CreatePatientGuestAsync(guest);

            Assert.NotNull(result);
            Assert.Equal("87654321", result.PatientIdentification);
        }

        [Fact]
        public async Task GetPatientGuestById_ReturnsGuest()
        {
            var seeded = await SeedGuestAsync("Guest To Find", "111222333");

            var result = await _sut.GetPatientGuestById(seeded.PatientIdentification);

            Assert.NotNull(result);
            Assert.Equal(seeded.PatientName, result!.PatientName);
        }

        [Fact]
        public async Task GetPatientGuestByQuery_ReturnsMatched()
        {
            var seeded = await SeedGuestAsync("Queryable Guest", "999888777");

            var result = await _sut.GetPatientGuestByQuery(seeded.PatientIdentification);

            Assert.NotNull(result);
            Assert.Contains(result!, g => g!.PatientIdentification == seeded.PatientIdentification);
        }

        [Fact]
        public async Task GetPatientProfileByQuery_ReturnsMatched()
        {
            var email = "patient@query.com";
            var user = await SeedPatientUserAsync("Patient User", email);

            var result = await _sut.GetPatientProfileByQueryAsync(email);

            Assert.NotNull(result);
            Assert.Contains(result!, p => p!.UserId == user.Id);
        }
    }
}
