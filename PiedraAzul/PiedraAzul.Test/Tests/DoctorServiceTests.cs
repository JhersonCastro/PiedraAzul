using Microsoft.EntityFrameworkCore;
using PiedraAzul.ApplicationServices.Services;
using PiedraAzul.Data;
using PiedraAzul.Data.Models;
using PiedraAzul.Shared.Enums;

namespace PiedraAzul.Test.Tests
{
    public class DoctorServiceTests : IClassFixture<PostgresFixture>
    {
        private readonly PostgresFixture _fixture;
        private readonly DoctorService _sut;

        public DoctorServiceTests(PostgresFixture fixture)
        {
            _fixture = fixture;
            _sut = new DoctorService(fixture.DbContextFactory);
        }

        private async Task<DoctorProfile> SeedDoctorAsync(string name, DoctorType specialty)
        {
            await using var ctx = _fixture.DbContextFactory.CreateDbContext();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = $"{name.ToLower().Replace(" ", "")}@test.com",
                Name = name
            };

            var doctor = new DoctorProfile
            {
                UserId = user.Id,
                Specialty = specialty
            };

            ctx.Users.Add(user);
            ctx.DoctorProfiles.Add(doctor);
            await ctx.SaveChangesAsync();

            return doctor;
        }

        [Fact]
        public async Task GetDoctorByUserId_ReturnsDoctor()
        {
            var seeded = await SeedDoctorAsync("Dr. Test", DoctorType.NaturalMedicine);

            var result = await _sut.GetDoctorByUserIdAsync(seeded.UserId);

            Assert.NotNull(result);
            Assert.Equal(seeded.UserId, result!.UserId);
        }

        [Fact]
        public async Task GetDoctorByType_ReturnsMatchedDoctors()
        {
            var seeded = await SeedDoctorAsync("Dr. Specialist", DoctorType.Physiotherapy);

            var result = await _sut.GetDoctorByTypeAsync(DoctorType.Physiotherapy);

            Assert.NotEmpty(result);
            Assert.Contains(result, d => d.UserId == seeded.UserId);
        }

        [Fact]
        public async Task GetDoctorByType_NotFound_ReturnsEmpty()
        {
            var result = await _sut.GetDoctorByTypeAsync(DoctorType.Optometry);

            Assert.Empty(result);
        }
    }
}
