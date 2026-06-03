using Moq;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.User;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorByUserId;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorDaySlots;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorsBySpecialty;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Entities.Shared.Enums;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Test.Tests;

public class DoctorServiceTests
{
    [Fact]
    public async Task GetDoctorByUserId_ReturnsDtoWhenDoctorAndUserExist()
    {
        var doctorRepo = new Mock<IDoctorRepository>();
        var identity = new Mock<IIdentityService>();

        doctorRepo.Setup(x => x.GetByIdAsync("doc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Doctor("doc-1", DoctorType.Physiotherapy, "L-01", "Notes"));
        identity.Setup(x => x.GetById("doc-1"))
            .ReturnsAsync(new UserDto("doc-1", "mail@test.com", "Dra Uno", "avatar.png"));

        var sut = new GetDoctorByUserIdHandler(doctorRepo.Object, identity.Object);

        var result = await sut.Handle(new GetDoctorByUserIdQuery("doc-1"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Dra Uno", result!.Name);
    }

    [Fact]
    public async Task GetDoctorByUserId_ReturnsNullWhenDoctorDoesNotExist()
    {
        var doctorRepo = new Mock<IDoctorRepository>();
        var identity = new Mock<IIdentityService>();

        doctorRepo.Setup(x => x.GetByIdAsync("doc-404", It.IsAny<CancellationToken>())).ReturnsAsync((Doctor?)null);

        var sut = new GetDoctorByUserIdHandler(doctorRepo.Object, identity.Object);

        var result = await sut.Handle(new GetDoctorByUserIdQuery("doc-404"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDoctorByUserId_ReturnsNullWhenIdentityUserDoesNotExist()
    {
        var doctorRepo = new Mock<IDoctorRepository>();
        var identity = new Mock<IIdentityService>();

        doctorRepo.Setup(x => x.GetByIdAsync("doc-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Doctor("doc-2", DoctorType.Optometry, "L-02", ""));
        identity.Setup(x => x.GetById("doc-2")).ReturnsAsync((UserDto?)null);

        var sut = new GetDoctorByUserIdHandler(doctorRepo.Object, identity.Object);

        var result = await sut.Handle(new GetDoctorByUserIdQuery("doc-2"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDoctorsBySpecialty_ReturnsMappedDoctors()
    {
        var doctorRepo = new Mock<IDoctorRepository>();
        var identity = new Mock<IIdentityService>();

        doctorRepo.Setup(x => x.GetBySpecialtyAsync(DoctorType.NaturalMedicine, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Doctor("doc-a", DoctorType.NaturalMedicine, "L-A", ""),
                new Doctor("doc-b", DoctorType.NaturalMedicine, "L-B", "")
            ]);

        identity.Setup(x => x.GetByIds(It.IsAny<List<string>>()))
            .ReturnsAsync([
                new UserDto("doc-a", "a@test.com", "Doc A", "a.png"),
                new UserDto("doc-b", "b@test.com", "Doc B", "b.png")
            ]);

        var sut = new GetDoctorsBySpecialtyHandler(doctorRepo.Object, identity.Object);

        var result = await sut.Handle(new GetDoctorsBySpecialtyQuery(DoctorType.NaturalMedicine), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetDoctorsBySpecialty_ReturnsEmptyWhenNoDoctors()
    {
        var doctorRepo = new Mock<IDoctorRepository>();
        var identity = new Mock<IIdentityService>();

        doctorRepo.Setup(x => x.GetBySpecialtyAsync(DoctorType.Physiotherapy, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = new GetDoctorsBySpecialtyHandler(doctorRepo.Object, identity.Object);

        var result = await sut.Handle(new GetDoctorsBySpecialtyQuery(DoctorType.Physiotherapy), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDoctorsBySpecialty_ExcludesDoctorsWithoutIdentityUser()
    {
        var doctorRepo = new Mock<IDoctorRepository>();
        var identity = new Mock<IIdentityService>();

        doctorRepo.Setup(x => x.GetBySpecialtyAsync(DoctorType.Optometry, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Doctor("doc-a", DoctorType.Optometry, "L-A", ""),
                new Doctor("doc-b", DoctorType.Optometry, "L-B", "")
            ]);

        identity.Setup(x => x.GetByIds(It.IsAny<List<string>>()))
            .ReturnsAsync([new UserDto("doc-a", "a@test.com", "Doc A", "a.png")]);

        var sut = new GetDoctorsBySpecialtyHandler(doctorRepo.Object, identity.Object);

        var result = await sut.Handle(new GetDoctorsBySpecialtyQuery(DoctorType.Optometry), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("doc-a", result[0].Id);
    }

    [Fact]
    public async Task GetDoctorDaySlots_ThrowsWhenDoctorDoesNotExist()
    {
        var doctorRepo = new Mock<IDoctorRepository>();
        var slotRepo = new Mock<IDoctorAvailabilitySlotRepository>();
        var appointmentRepo = new Mock<IAppointmentRepository>();

        doctorRepo.Setup(x => x.ExistsAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = new GetDoctorDaySlotsHandler(doctorRepo.Object, slotRepo.Object, appointmentRepo.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.Handle(new GetDoctorDaySlotsQuery("missing", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task GetDoctorDaySlots_ReturnsAvailabilityFromAppointments()
    {
        var doctorRepo = new Mock<IDoctorRepository>();
        var slotRepo = new Mock<IDoctorAvailabilitySlotRepository>();
        var appointmentRepo = new Mock<IAppointmentRepository>();

        var date = NextDateFor(DayOfWeek.Tuesday);
        var slot1 = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Tuesday, TimeSpan.FromHours(8), TimeSpan.FromHours(9));
        var slot2 = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Tuesday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));

        doctorRepo.Setup(x => x.ExistsAsync("doc-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        slotRepo.Setup(x => x.ListByDoctorAsync("doc-1", It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync([slot1, slot2]);

        var busy = Appointment.Create(slot1, date, "doc-1", "user-1", null);
        appointmentRepo.Setup(x => x.ListByDoctorAsync("doc-1", date, It.IsAny<CancellationToken>())).ReturnsAsync([busy]);

        var sut = new GetDoctorDaySlotsHandler(doctorRepo.Object, slotRepo.Object, appointmentRepo.Object);

        var result = await sut.Handle(new GetDoctorDaySlotsQuery("doc-1", date), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.False(result.Single(x => x.Id == slot1.Id).IsAvailable);
        Assert.True(result.Single(x => x.Id == slot2.Id).IsAvailable);
    }



    [Fact]
    public async Task GetDoctorDaySlots_FiltersByMatchingDayOfWeekAndPreservesRanges()
    {
        var doctorRepo = new Mock<IDoctorRepository>();
        var slotRepo = new Mock<IDoctorAvailabilitySlotRepository>();
        var appointmentRepo = new Mock<IAppointmentRepository>();

        var targetDate = NextDateFor(DayOfWeek.Wednesday);
        var validMorning = new DoctorAvailabilitySlot("doc-2", DayOfWeek.Wednesday, TimeSpan.FromHours(7), TimeSpan.FromHours(8));
        var validNoon = new DoctorAvailabilitySlot("doc-2", DayOfWeek.Wednesday, TimeSpan.FromHours(12), TimeSpan.FromHours(13));
        var invalidDay = new DoctorAvailabilitySlot("doc-2", DayOfWeek.Thursday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));

        doctorRepo.Setup(x => x.ExistsAsync("doc-2", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        slotRepo.Setup(x => x.ListByDoctorAsync("doc-2", It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([validMorning, validNoon, invalidDay]);
        appointmentRepo.Setup(x => x.ListByDoctorAsync("doc-2", targetDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = new GetDoctorDaySlotsHandler(doctorRepo.Object, slotRepo.Object, appointmentRepo.Object);

        var result = await sut.Handle(new GetDoctorDaySlotsQuery("doc-2", targetDate), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, x => x.Id == invalidDay.Id);

        var morning = result.Single(x => x.Id == validMorning.Id);
        var noon = result.Single(x => x.Id == validNoon.Id);

        Assert.Equal(TimeSpan.FromHours(7), morning.StartTime);
        Assert.Equal(TimeSpan.FromHours(8), morning.EndTime);
        Assert.Equal(TimeSpan.FromHours(12), noon.StartTime);
        Assert.Equal(TimeSpan.FromHours(13), noon.EndTime);
        Assert.All(result, x => Assert.True(x.IsAvailable));
    }



    [Fact]
    public async Task GetDoctorDaySlots_WhenDateDoesNotMatchAnySlot_ReturnsEmpty()
    {
        var doctorRepo = new Mock<IDoctorRepository>();
        var slotRepo = new Mock<IDoctorAvailabilitySlotRepository>();
        var appointmentRepo = new Mock<IAppointmentRepository>();

        var targetDate = NextDateFor(DayOfWeek.Sunday);
        var mondaySlot = new DoctorAvailabilitySlot("doc-3", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        var tuesdaySlot = new DoctorAvailabilitySlot("doc-3", DayOfWeek.Tuesday, TimeSpan.FromHours(10), TimeSpan.FromHours(11));

        doctorRepo.Setup(x => x.ExistsAsync("doc-3", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        slotRepo.Setup(x => x.ListByDoctorAsync("doc-3", It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([mondaySlot, tuesdaySlot]);
        appointmentRepo.Setup(x => x.ListByDoctorAsync("doc-3", targetDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = new GetDoctorDaySlotsHandler(doctorRepo.Object, slotRepo.Object, appointmentRepo.Object);

        var result = await sut.Handle(new GetDoctorDaySlotsQuery("doc-3", targetDate), CancellationToken.None);

        Assert.Empty(result);
    }

    private static DateOnly NextDateFor(DayOfWeek day)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1));
        while (date.DayOfWeek != day)
            date = date.AddDays(1);
        return date;
    }
}
