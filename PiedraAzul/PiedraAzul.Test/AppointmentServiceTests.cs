using Microsoft.EntityFrameworkCore;
using PiedraAzul.ApplicationServices.Services;
using PiedraAzul.Data;
using PiedraAzul.Data.Models;
using PiedraAzul.Shared.Enums;

namespace PiedraAzul.Test;

public class AppointmentServiceTests
{
    [Fact]
    public async Task SearchDoctorAppointmentsAsync_ShouldReturnPagedResultsOrderedByStartTime()
    {
        var factory = BuildFactory(nameof(SearchDoctorAppointmentsAsync_ShouldReturnPagedResultsOrderedByStartTime));
        await SeedAsync(factory);
        var sut = new AppointmentService(factory);

        var date = new DateTime(2026, 03, 25, 12, 0, 0, DateTimeKind.Utc);

        var result = await sut.SearchDoctorAppointmentsAsync("doctor-user-1", date, 1, 1);

        Assert.Equal(2, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("09:00 - 09:30", result.Items[0].TimeRange);
        Assert.Equal("Paciente Uno", result.Items[0].Patient);
        Assert.Equal("Registrado", result.Items[0].PatientType);
        Assert.Equal("Physiotherapy", result.Items[0].Specialty);
    }

    [Fact]
    public async Task SearchDoctorAppointmentsAsync_ShouldReturnGuestPatientAndProgrammedStatus()
    {
        var factory = BuildFactory(nameof(SearchDoctorAppointmentsAsync_ShouldReturnGuestPatientAndProgrammedStatus));
        await SeedAsync(factory);
        var sut = new AppointmentService(factory);

        var date = new DateTime(2099, 03, 25, 12, 0, 0, DateTimeKind.Utc);

        var result = await sut.SearchDoctorAppointmentsAsync("doctor-user-1", date, 1, 10);

        Assert.Single(result.Items);
        Assert.Equal("Invitado", result.Items[0].PatientType);
        Assert.Contains("Invitado Uno", result.Items[0].Patient);
        Assert.Equal("Programada", result.Items[0].Status);
    }

    private static TestDbContextFactory BuildFactory(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new TestDbContextFactory(options);
    }

    private static async Task SeedAsync(TestDbContextFactory factory)
    {
        await using var context = await factory.CreateDbContextAsync();

        // 🔹 Usuarios
        var doctorUser = new ApplicationUser
        {
            Id = "doctor-user-1",
            UserName = "doctor@example.com",
            Name = "Doctor Uno",
            Email = "doctor@example.com"
        };

        var patientUser = new ApplicationUser
        {
            Id = "patient-user-1",
            UserName = "patient@example.com",
            Name = "Paciente Uno",
            Email = "patient@example.com"
        };

        // 🔹 Perfiles
        var doctorProfile = new DoctorProfile
        {
            UserId = doctorUser.Id,
            User = doctorUser,
            Specialty = DoctorType.Physiotherapy,
            LicenseNumber = "MED-123"
        };

        var patientProfile = new PatientProfile
        {
            UserId = patientUser.Id,
            User = patientUser
        };

        // 🔹 Invitado
        var guest = new PatientGuest
        {
            PatientIdentification = "CC123",
            PatientName = "Invitado Uno",
            PatientPhone = "3000000000"
        };

        // 🔹 Slots (AHORA usan DoctorUserId)
        var slotA = new DoctorAvailabilitySlot
        {
            Id = Guid.NewGuid(),
            DoctorUserId = doctorUser.Id,
            DayOfWeek = DayOfWeek.Wednesday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(9, 30, 0)
        };

        var slotB = new DoctorAvailabilitySlot
        {
            Id = Guid.NewGuid(),
            DoctorUserId = doctorUser.Id,
            DayOfWeek = DayOfWeek.Wednesday,
            StartTime = new TimeSpan(11, 0, 0),
            EndTime = new TimeSpan(11, 30, 0)
        };

        context.Users.AddRange(doctorUser, patientUser);
        context.DoctorProfiles.Add(doctorProfile);
        context.PatientProfiles.Add(patientProfile);
        context.PatientGuests.Add(guest);
        context.DoctorAvailabilitySlots.AddRange(slotA, slotB);

        // 🔹 Citas (ACTUALIZADAS)
        context.Appointments.AddRange(
            new Appointment
            {
                Id = Guid.NewGuid(),
                DoctorUserId = doctorUser.Id,
                PatientUserId = patientUser.Id,
                Date = new DateTime(2026, 03, 25, 5, 0, 0, DateTimeKind.Utc),
                DoctorAvailabilitySlotId = slotA.Id,
                CreatedAt = new DateTime(2026, 03, 20, 13, 0, 0, DateTimeKind.Utc)
            },
            new Appointment
            {
                Id = Guid.NewGuid(),
                DoctorUserId = doctorUser.Id,
                PatientUserId = patientUser.Id,
                Date = new DateTime(2026, 03, 25, 5, 0, 0, DateTimeKind.Utc),
                DoctorAvailabilitySlotId = slotB.Id,
                CreatedAt = new DateTime(2026, 03, 20, 14, 0, 0, DateTimeKind.Utc)
            },
            new Appointment
            {
                Id = Guid.NewGuid(),
                DoctorUserId = doctorUser.Id,
                PatientGuestId = guest.PatientIdentification,
                Date = new DateTime(2099, 03, 25, 5, 0, 0, DateTimeKind.Utc),
                DoctorAvailabilitySlotId = slotA.Id,
                CreatedAt = new DateTime(2099, 03, 20, 14, 0, 0, DateTimeKind.Utc)
            });

        await context.SaveChangesAsync();
    }

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppDbContext(options));
    }
}