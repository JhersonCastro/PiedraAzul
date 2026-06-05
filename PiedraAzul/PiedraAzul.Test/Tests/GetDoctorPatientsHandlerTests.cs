using Moq;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.Patients;
using PiedraAzul.Application.Common.Models.User;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorPatients;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Entities.Profiles.Patients;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Test.Tests;

public class GetDoctorPatientsHandlerTests
{
    private readonly Mock<IAppointmentRepository> _appointmentRepository = new();
    private readonly Mock<IPatientGuestRepository> _guestRepository = new();
    private readonly Mock<IIdentityService> _identityService = new();

    private readonly GetDoctorPatientsHandler _sut;

    public GetDoctorPatientsHandlerTests()
    {
        _sut = new GetDoctorPatientsHandler(
            _appointmentRepository.Object,
            _guestRepository.Object,
            _identityService.Object);

        // Defaults vacíos (cada test sobreescribe lo que necesita).
        _identityService
            .Setup(x => x.GetPatientUsersByIds(It.IsAny<IReadOnlyCollection<string>>()))
            .ReturnsAsync([]);
        _guestRepository
            .Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    [Fact]
    public async Task GetDoctorPatients_WhenNoAppointments_ReturnsEmpty()
    {
        SetupAppointments();

        var result = await _sut.Handle(new GetDoctorPatientsQuery("doc-1"), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDoctorPatients_ReturnsRegisteredPatient_WithIdentityData()
    {
        var date = NextDateFor(DayOfWeek.Monday);
        SetupAppointments(RegisteredAppointment("user-1", date));
        _identityService
            .Setup(x => x.GetPatientUsersByIds(It.IsAny<IReadOnlyCollection<string>>()))
            .ReturnsAsync([new PatientUserDto("user-1", "Ana", "CC100", "300100")]);

        var result = await _sut.Handle(new GetDoctorPatientsQuery("doc-1"), CancellationToken.None);

        var p = Assert.Single(result);
        Assert.Equal("user-1", p.Id);
        Assert.Equal("Ana", p.Name);
        Assert.Equal("CC100", p.Identification);
        Assert.Equal("300100", p.Phone);
        Assert.Equal(PatientKind.Registered, p.Type);
        Assert.Equal(date.ToDateTime(TimeOnly.MinValue), p.LastVisit);
    }

    [Fact]
    public async Task GetDoctorPatients_ReturnsGuestPatient()
    {
        var date = NextDateFor(DayOfWeek.Monday);
        SetupAppointments(GuestAppointment("CC200", date));
        _guestRepository
            .Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new GuestPatient("CC200", "Beto", "300200", "")]);

        var result = await _sut.Handle(new GetDoctorPatientsQuery("doc-1"), CancellationToken.None);

        var p = Assert.Single(result);
        Assert.Equal("CC200", p.Id);
        Assert.Equal("Beto", p.Name);
        Assert.Equal("CC200", p.Identification);
        Assert.Equal("300200", p.Phone);
        Assert.Equal(PatientKind.Guest, p.Type);
    }

    [Fact]
    public async Task GetDoctorPatients_DeduplicatesGuestWithSameIdentificationAsRegistered()
    {
        var dRegistered = NextDateFor(DayOfWeek.Monday);
        var dGuestLater = dRegistered.AddDays(7);

        // Registrado user-1 con cédula CC300; un guest con la misma cédula y visita más reciente.
        SetupAppointments(
            RegisteredAppointment("user-1", dRegistered),
            GuestAppointment("CC300", dGuestLater));

        _identityService
            .Setup(x => x.GetPatientUsersByIds(It.IsAny<IReadOnlyCollection<string>>()))
            .ReturnsAsync([new PatientUserDto("user-1", "Ana", "CC300", "300300")]);
        _guestRepository
            .Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new GuestPatient("CC300", "Ana Guest", "300300", "")]);

        var result = await _sut.Handle(new GetDoctorPatientsQuery("doc-1"), CancellationToken.None);

        // El guest se fusiona en el registrado: una sola entrada.
        var p = Assert.Single(result);
        Assert.Equal("user-1", p.Id);
        Assert.Equal(PatientKind.Registered, p.Type);
        // La última visita se actualiza a la del guest (más reciente).
        Assert.Equal(dGuestLater.ToDateTime(TimeOnly.MinValue), p.LastVisit);
    }

    [Fact]
    public async Task GetDoctorPatients_OrdersByName()
    {
        var date = NextDateFor(DayOfWeek.Monday);
        SetupAppointments(
            RegisteredAppointment("user-z", date),
            RegisteredAppointment("user-a", date));
        _identityService
            .Setup(x => x.GetPatientUsersByIds(It.IsAny<IReadOnlyCollection<string>>()))
            .ReturnsAsync(
            [
                new PatientUserDto("user-z", "Zeta", "CCZ", ""),
                new PatientUserDto("user-a", "Alpha", "CCA", "")
            ]);

        var result = await _sut.Handle(new GetDoctorPatientsQuery("doc-1"), CancellationToken.None);

        Assert.Equal(["Alpha", "Zeta"], result.Select(p => p.Name));
    }

    [Fact]
    public async Task GetDoctorPatients_WhenRegisteredUserMissingFromIdentity_FallsBackToId()
    {
        var date = NextDateFor(DayOfWeek.Monday);
        SetupAppointments(RegisteredAppointment("user-ghost", date));
        // Identity no devuelve al usuario (p.ej. eliminado): se usa el id como nombre.

        var result = await _sut.Handle(new GetDoctorPatientsQuery("doc-1"), CancellationToken.None);

        var p = Assert.Single(result);
        Assert.Equal("user-ghost", p.Id);
        Assert.Equal("user-ghost", p.Name);
        Assert.Equal("", p.Identification);
    }

    private void SetupAppointments(params Appointment[] appointments) =>
        _appointmentRepository
            .Setup(x => x.ListByDoctorAsync("doc-1", It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointments);

    private static Appointment RegisteredAppointment(string userId, DateOnly date)
    {
        var slot = new DoctorAvailabilitySlot("doc-1", date.DayOfWeek, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        return Appointment.Create(slot, date, "doc-1", userId, null);
    }

    private static Appointment GuestAppointment(string guestId, DateOnly date)
    {
        var slot = new DoctorAvailabilitySlot("doc-1", date.DayOfWeek, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        return Appointment.Create(slot, date, "doc-1", null, guestId);
    }

    private static DateOnly NextDateFor(DayOfWeek day)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1));
        while (date.DayOfWeek != day)
            date = date.AddDays(1);
        return date;
    }
}
