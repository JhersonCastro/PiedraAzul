using Moq;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.User;
using PiedraAzul.Application.Features.Patients.Queries.GetGuestById;
using PiedraAzul.Application.Features.Patients.Queries.GetPatientAppointments;
using PiedraAzul.Application.Features.Patients.Queries.GetRegisteredPatientByUserId;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Entities.Profiles.Patients;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Test.Tests;

public class PatientServiceTests
{
    [Fact]
    public async Task GetGuestPatientById_ReturnsGuestDto()
    {
        var repo = new Mock<IPatientGuestRepository>();
        repo.Setup(x => x.GetByIdAsync("g-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuestPatient("g-1", "Guest Name", "300", ""));

        var sut = new GetGuestPatientByIdHandler(repo.Object);

        var result = await sut.Handle(new GetGuestPatientByIdQuery("g-1"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Guest", result!.Type);
    }

    [Fact]
    public async Task GetRegisteredPatientByUserId_ReturnsRegisteredDto()
    {
        var repo = new Mock<IPatientRepository>();
        repo.Setup(x => x.GetByUserIdAsync("u-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegisteredPatient("u-1", "Reg Name"));

        var sut = new GetRegisteredPatientByUserIdHandler(repo.Object);

        var result = await sut.Handle(new GetRegisteredPatientByUserIdQuery("u-1"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Registered", result!.Type);
    }

    [Fact]
    public async Task GetPatientAppointments_ThrowsWhenBothIdsAreNull()
    {
        var patientRepo = new Mock<IPatientRepository>();
        var guestRepo = new Mock<IPatientGuestRepository>();
        var appointmentRepo = new Mock<IAppointmentRepository>();
        var identity = new Mock<IIdentityService>();
        var doctorRepo = new Mock<IDoctorRepository>();
        var slotRepo = new Mock<IDoctorAvailabilitySlotRepository>();

        var sut = new GetPatientAppointmentsHandler(patientRepo.Object, guestRepo.Object, appointmentRepo.Object, identity.Object, doctorRepo.Object, slotRepo.Object, new Mock<IAppointmentRescheduleRecordRepository>().Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.Handle(new GetPatientAppointmentsQuery(null, null), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task GetPatientAppointments_ReturnsRegisteredAppointments()
    {
        var patientRepo = new Mock<IPatientRepository>();
        var guestRepo = new Mock<IPatientGuestRepository>();
        var appointmentRepo = new Mock<IAppointmentRepository>();
        var identity = new Mock<IIdentityService>();
        var doctorRepo = new Mock<IDoctorRepository>();
        var slotRepo = new Mock<IDoctorAvailabilitySlotRepository>();

        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var slot = new DoctorAvailabilitySlot("doc", date.DayOfWeek, TimeSpan.FromHours(8), TimeSpan.FromHours(9));
        var appointment = Appointment.Create(slot, date, "doc", "u-2", null);

        appointmentRepo.Setup(x => x.ListByPatientUserAsync("u-2", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([appointment]);
        identity.Setup(x => x.GetByIds(It.IsAny<List<string>>()))
            .ReturnsAsync([new UserDto("u-2", "u2@test.com", "Paciente", ""), new UserDto("doc", "doc@test.com", "Dr. Test", "")]);
        guestRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        doctorRepo.Setup(x => x.GetByIdAsync("doc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Doctor("doc", PiedraAzul.Domain.Entities.Shared.Enums.DoctorType.NaturalMedicine, "LIC-01", ""));
        slotRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([slot]);

        var sut = new GetPatientAppointmentsHandler(patientRepo.Object, guestRepo.Object, appointmentRepo.Object, identity.Object, doctorRepo.Object, slotRepo.Object, new Mock<IAppointmentRescheduleRecordRepository>().Object);

        var result = await sut.Handle(new GetPatientAppointmentsQuery("u-2", null), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Paciente", result[0].PatientName);
        Assert.Equal("Registered", result[0].PatientType);
        Assert.Equal("NaturalMedicine", result[0].Specialty);
        Assert.Equal(8, result[0].Start.Hour); // hora del slot = 08:00
    }
}
