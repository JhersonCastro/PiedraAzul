using Moq;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.User;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorAppointments;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Entities.Profiles.Patients;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Test.Tests;

public class RefreshTokenServiceTests
{
    [Fact]
    public async Task GetDoctorAppointments_ReturnsEmpty_WhenNoAppointments()
    {
        var appointmentRepo = new Mock<IAppointmentRepository>();
        var slotRepo = new Mock<IDoctorAvailabilitySlotRepository>();
        var identity = new Mock<IIdentityService>();
        var guestRepo = new Mock<IPatientGuestRepository>();

        appointmentRepo.Setup(x => x.ListByDoctorAsync("doc", null, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var sut = new GetDoctorAppointmentsHandler(appointmentRepo.Object, slotRepo.Object, identity.Object, guestRepo.Object);

        var result = await sut.Handle(new GetDoctorAppointmentsQuery("doc"), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDoctorAppointments_MapsRegisteredPatient()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var slot = new DoctorAvailabilitySlot("doc", date.DayOfWeek, TimeSpan.FromHours(8), TimeSpan.FromHours(9));
        var appointment = Appointment.Create(slot, date, "doc", "user-1", null);

        var appointmentRepo = new Mock<IAppointmentRepository>();
        var slotRepo = new Mock<IDoctorAvailabilitySlotRepository>();
        var identity = new Mock<IIdentityService>();
        var guestRepo = new Mock<IPatientGuestRepository>();

        appointmentRepo.Setup(x => x.ListByDoctorAsync("doc", date, It.IsAny<CancellationToken>())).ReturnsAsync([appointment]);
        slotRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([slot]);
        identity.Setup(x => x.GetByIds(It.IsAny<List<string>>())).ReturnsAsync([new UserDto("user-1", "u@test.com", "User Uno", "")]);
        guestRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var sut = new GetDoctorAppointmentsHandler(appointmentRepo.Object, slotRepo.Object, identity.Object, guestRepo.Object);

        var result = await sut.Handle(new GetDoctorAppointmentsQuery("doc", date), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Registered", result[0].PatientType);
        Assert.Equal("User Uno", result[0].PatientName);
    }

    [Fact]
    public async Task GetDoctorAppointments_MapsGuestPatient()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var slot = new DoctorAvailabilitySlot("doc", date.DayOfWeek, TimeSpan.FromHours(10), TimeSpan.FromHours(11));
        var appointment = Appointment.Create(slot, date, "doc", null, "guest-1");

        var appointmentRepo = new Mock<IAppointmentRepository>();
        var slotRepo = new Mock<IDoctorAvailabilitySlotRepository>();
        var identity = new Mock<IIdentityService>();
        var guestRepo = new Mock<IPatientGuestRepository>();

        appointmentRepo.Setup(x => x.ListByDoctorAsync("doc", date, It.IsAny<CancellationToken>())).ReturnsAsync([appointment]);
        slotRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([slot]);
        identity.Setup(x => x.GetByIds(It.IsAny<List<string>>())).ReturnsAsync([]);
        guestRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new GuestPatient("guest-1", "Guest Uno", "300", "")]);

        var sut = new GetDoctorAppointmentsHandler(appointmentRepo.Object, slotRepo.Object, identity.Object, guestRepo.Object);

        var result = await sut.Handle(new GetDoctorAppointmentsQuery("doc", date), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Guest", result[0].PatientType);
        Assert.Equal("Guest Uno", result[0].PatientName);
    }

    [Fact]
    public async Task GetDoctorAppointments_UsesSlotStartTimeForStartField()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var slot = new DoctorAvailabilitySlot("doc", date.DayOfWeek, TimeSpan.FromHours(13), TimeSpan.FromHours(14));
        var appointment = Appointment.Create(slot, date, "doc", "user-2", null);

        var appointmentRepo = new Mock<IAppointmentRepository>();
        var slotRepo = new Mock<IDoctorAvailabilitySlotRepository>();
        var identity = new Mock<IIdentityService>();
        var guestRepo = new Mock<IPatientGuestRepository>();

        appointmentRepo.Setup(x => x.ListByDoctorAsync("doc", date, It.IsAny<CancellationToken>())).ReturnsAsync([appointment]);
        slotRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([slot]);
        identity.Setup(x => x.GetByIds(It.IsAny<List<string>>())).ReturnsAsync([new UserDto("user-2", "u2@test.com", "User Dos", "")]);
        guestRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var sut = new GetDoctorAppointmentsHandler(appointmentRepo.Object, slotRepo.Object, identity.Object, guestRepo.Object);

        var result = await sut.Handle(new GetDoctorAppointmentsQuery("doc", date), CancellationToken.None);

        Assert.Equal(date.ToDateTime(TimeOnly.MinValue).AddHours(13), result[0].Start);
    }

    [Fact]
    public async Task GetDoctorAppointments_CallsDependenciesOnce()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var slot = new DoctorAvailabilitySlot("doc", date.DayOfWeek, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        var appointment = Appointment.Create(slot, date, "doc", "user-3", null);

        var appointmentRepo = new Mock<IAppointmentRepository>();
        var slotRepo = new Mock<IDoctorAvailabilitySlotRepository>();
        var identity = new Mock<IIdentityService>();
        var guestRepo = new Mock<IPatientGuestRepository>();

        appointmentRepo.Setup(x => x.ListByDoctorAsync("doc", date, It.IsAny<CancellationToken>())).ReturnsAsync([appointment]);
        slotRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([slot]);
        identity.Setup(x => x.GetByIds(It.IsAny<List<string>>())).ReturnsAsync([new UserDto("user-3", "u3@test.com", "User Tres", "")]);
        guestRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var sut = new GetDoctorAppointmentsHandler(appointmentRepo.Object, slotRepo.Object, identity.Object, guestRepo.Object);
        await sut.Handle(new GetDoctorAppointmentsQuery("doc", date), CancellationToken.None);

        appointmentRepo.Verify(x => x.ListByDoctorAsync("doc", date, It.IsAny<CancellationToken>()), Times.Once);
        slotRepo.Verify(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
