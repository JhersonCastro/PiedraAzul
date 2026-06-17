using Mediator;
using Moq;
using PiedraAzul.Application.Common.Models.Patients;
using PiedraAzul.Application.Features.Appointments.CreateAppointment;
using PiedraAzul.Application.Features.Patients.Commands.CreateGuestPatient;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Entities.Profiles.Patients;
using PiedraAzul.Domain.Entities.Shared.Enums;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Test.Tests;

public class AppointmentServiceTests
{
    private readonly Mock<IAppointmentRepository> _appointmentRepository = new();
    private readonly Mock<IDoctorRepository> _doctorRepository = new();
    private readonly Mock<IDoctorAvailabilitySlotRepository> _slotRepository = new();
    private readonly Mock<IPatientRepository> _patientRepository = new();
    private readonly Mock<IPatientGuestRepository> _guestRepository = new();
    private readonly Mock<IMediator> _mediator = new();

    private readonly CreateAppointmentHandler _sut;

    public AppointmentServiceTests()
    {
        _sut = new CreateAppointmentHandler(
            _appointmentRepository.Object,
            _doctorRepository.Object,
            _slotRepository.Object,
            _patientRepository.Object,
            _guestRepository.Object,
            new ImmediateUnitOfWork(),
            _mediator.Object);
    }

    [Fact]
    public async Task CreateAppointment_WithRegisteredPatient_CreatesAppointment()
    {
        var doctor = BuildDoctor();
        var slot = BuildSlot(doctor.Id);
        var date = NextDateFor(slot.DayOfWeek);

        _doctorRepository.Setup(x => x.GetByIdAsync(doctor.Id, It.IsAny<CancellationToken>())).ReturnsAsync(doctor);
        _slotRepository.Setup(x => x.GetByIdAsync(slot.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slot);
        _patientRepository.Setup(x => x.GetByUserIdAsync("patient-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegisteredPatient("patient-1", "Juan"));
        _appointmentRepository.Setup(x => x.ExistsBySlotAndDateAsync(slot.Id, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new CreateAppointmentCommand(doctor.Id, slot.Id, date, "patient-1", null);

        var result = await _sut.Handle(request, CancellationToken.None);

        Assert.Equal("patient-1", result.PatientUserId);
        Assert.Null(result.PatientGuestId);
    }

    [Fact]
    public async Task CreateAppointment_WhenDoctorNotFound_Throws()
    {
        _doctorRepository.Setup(x => x.GetByIdAsync("doctor-1", It.IsAny<CancellationToken>())).ReturnsAsync((Doctor?)null);

        var request = new CreateAppointmentCommand("doctor-1", Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), "user", null);

        var ex = await Assert.ThrowsAsync<Exception>(() => _sut.Handle(request, CancellationToken.None).AsTask());

        Assert.Equal("Doctor not found", ex.Message);
    }

    [Fact]
    public async Task CreateAppointment_WhenSlotNotFound_Throws()
    {
        var doctor = BuildDoctor();
        _doctorRepository.Setup(x => x.GetByIdAsync(doctor.Id, It.IsAny<CancellationToken>())).ReturnsAsync(doctor);
        _slotRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DoctorAvailabilitySlot?)null);

        var request = new CreateAppointmentCommand(doctor.Id, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), "user", null);

        var ex = await Assert.ThrowsAsync<Exception>(() => _sut.Handle(request, CancellationToken.None).AsTask());

        Assert.Equal("Slot not found", ex.Message);
    }

    [Fact]
    public async Task CreateAppointment_WhenPatientMissing_Throws()
    {
        var doctor = BuildDoctor();
        var slot = BuildSlot(doctor.Id);

        _doctorRepository.Setup(x => x.GetByIdAsync(doctor.Id, It.IsAny<CancellationToken>())).ReturnsAsync(doctor);
        _slotRepository.Setup(x => x.GetByIdAsync(slot.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slot);

        var request = new CreateAppointmentCommand(doctor.Id, slot.Id, NextDateFor(slot.DayOfWeek), null, null);

        var ex = await Assert.ThrowsAsync<Exception>(() => _sut.Handle(request, CancellationToken.None).AsTask());

        Assert.Equal("Patient required", ex.Message);
    }

    [Fact]
    public async Task CreateAppointment_WhenRegisteredPatientNotFound_Throws()
    {
        var doctor = BuildDoctor();
        var slot = BuildSlot(doctor.Id);

        _doctorRepository.Setup(x => x.GetByIdAsync(doctor.Id, It.IsAny<CancellationToken>())).ReturnsAsync(doctor);
        _slotRepository.Setup(x => x.GetByIdAsync(slot.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slot);
        _patientRepository.Setup(x => x.GetByUserIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((RegisteredPatient?)null);

        var request = new CreateAppointmentCommand(doctor.Id, slot.Id, NextDateFor(slot.DayOfWeek), "missing", null);

        var ex = await Assert.ThrowsAsync<Exception>(() => _sut.Handle(request, CancellationToken.None).AsTask());

        Assert.Equal("Patient not found", ex.Message);
    }

    [Fact]
    public async Task CreateAppointment_WithExistingGuest_UsesGuestId()
    {
        var doctor = BuildDoctor();
        var slot = BuildSlot(doctor.Id);
        var date = NextDateFor(slot.DayOfWeek);
        var guest = new GuestPatient("guest-1", "Invitado", "300", "");

        _doctorRepository.Setup(x => x.GetByIdAsync(doctor.Id, It.IsAny<CancellationToken>())).ReturnsAsync(doctor);
        _slotRepository.Setup(x => x.GetByIdAsync(slot.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slot);
        _guestRepository.Setup(x => x.GetByIdAsync("id-001", It.IsAny<CancellationToken>())).ReturnsAsync(guest);
        _appointmentRepository.Setup(x => x.ExistsBySlotAndDateAsync(slot.Id, date, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var request = new CreateAppointmentCommand(doctor.Id, slot.Id, date, null, new GuestPatientRequest
        {
            Identification = "id-001",
            Name = "Invitado",
            Phone = "300"
        });

        var result = await _sut.Handle(request, CancellationToken.None);

        Assert.Equal("guest-1", result.PatientGuestId);
    }

    [Fact]
    public async Task CreateAppointment_WithNewGuest_CreatesGuestViaMediator()
    {
        var doctor = BuildDoctor();
        var slot = BuildSlot(doctor.Id);
        var date = NextDateFor(slot.DayOfWeek);

        _doctorRepository.Setup(x => x.GetByIdAsync(doctor.Id, It.IsAny<CancellationToken>())).ReturnsAsync(doctor);
        _slotRepository.Setup(x => x.GetByIdAsync(slot.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slot);
        _guestRepository.Setup(x => x.GetByIdAsync("id-002", It.IsAny<CancellationToken>())).ReturnsAsync((GuestPatient?)null);
        _mediator.Setup(x => x.Send(It.IsAny<CreateGuestPatientCommand>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult("guest-created"));
        _appointmentRepository.Setup(x => x.ExistsBySlotAndDateAsync(slot.Id, date, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var request = new CreateAppointmentCommand(doctor.Id, slot.Id, date, null, new GuestPatientRequest
        {
            Identification = "id-002",
            Name = "Nuevo",
            Phone = "301"
        });

        var result = await _sut.Handle(request, CancellationToken.None);

        Assert.Equal("guest-created", result.PatientGuestId);
    }

    [Fact]
    public async Task CreateAppointment_WhenGuestCreationFails_Throws()
    {
        var doctor = BuildDoctor();
        var slot = BuildSlot(doctor.Id);

        _doctorRepository.Setup(x => x.GetByIdAsync(doctor.Id, It.IsAny<CancellationToken>())).ReturnsAsync(doctor);
        _slotRepository.Setup(x => x.GetByIdAsync(slot.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slot);
        _guestRepository.Setup(x => x.GetByIdAsync("id-003", It.IsAny<CancellationToken>())).ReturnsAsync((GuestPatient?)null);
        _mediator.Setup(x => x.Send(It.IsAny<CreateGuestPatientCommand>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<string>(null!));

        var request = new CreateAppointmentCommand(doctor.Id, slot.Id, NextDateFor(slot.DayOfWeek), null, new GuestPatientRequest
        {
            Identification = "id-003",
            Name = "Nuevo",
            Phone = "301"
        });

        var ex = await Assert.ThrowsAsync<Exception>(() => _sut.Handle(request, CancellationToken.None).AsTask());

        Assert.Equal("Failed to create guest patient", ex.Message);
    }

    [Fact]
    public async Task CreateAppointment_WhenSlotAlreadyTaken_Throws()
    {
        var doctor = BuildDoctor();
        var slot = BuildSlot(doctor.Id);
        var date = NextDateFor(slot.DayOfWeek);

        _doctorRepository.Setup(x => x.GetByIdAsync(doctor.Id, It.IsAny<CancellationToken>())).ReturnsAsync(doctor);
        _slotRepository.Setup(x => x.GetByIdAsync(slot.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slot);
        _patientRepository.Setup(x => x.GetByUserIdAsync("patient-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegisteredPatient("patient-2", "Ana"));
        _appointmentRepository.Setup(x => x.ExistsBySlotAndDateAsync(slot.Id, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CreateAppointmentCommand(doctor.Id, slot.Id, date, "patient-2", null);

        var ex = await Assert.ThrowsAsync<Exception>(() => _sut.Handle(request, CancellationToken.None).AsTask());

        Assert.Equal("Slot already taken", ex.Message);
    }

    [Fact]
    public async Task CreateAppointment_AddsAppointmentToRepository()
    {
        var doctor = BuildDoctor();
        var slot = BuildSlot(doctor.Id);
        var date = NextDateFor(slot.DayOfWeek);

        _doctorRepository.Setup(x => x.GetByIdAsync(doctor.Id, It.IsAny<CancellationToken>())).ReturnsAsync(doctor);
        _slotRepository.Setup(x => x.GetByIdAsync(slot.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slot);
        _patientRepository.Setup(x => x.GetByUserIdAsync("patient-3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegisteredPatient("patient-3", "Mar"));
        _appointmentRepository.Setup(x => x.ExistsBySlotAndDateAsync(slot.Id, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new CreateAppointmentCommand(doctor.Id, slot.Id, date, "patient-3", null);
        await _sut.Handle(request, CancellationToken.None);

        _appointmentRepository.Verify(x => x.AddAsync(It.IsAny<PiedraAzul.Domain.Entities.Operations.Appointment>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAppointment_UsesGivenDate()
    {
        var doctor = BuildDoctor();
        var slot = BuildSlot(doctor.Id, DayOfWeek.Friday);
        var date = NextDateFor(DayOfWeek.Friday);

        _doctorRepository.Setup(x => x.GetByIdAsync(doctor.Id, It.IsAny<CancellationToken>())).ReturnsAsync(doctor);
        _slotRepository.Setup(x => x.GetByIdAsync(slot.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slot);
        _patientRepository.Setup(x => x.GetByUserIdAsync("patient-4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegisteredPatient("patient-4", "Leo"));
        _appointmentRepository.Setup(x => x.ExistsBySlotAndDateAsync(slot.Id, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new CreateAppointmentCommand(doctor.Id, slot.Id, date, "patient-4", null);
        var result = await _sut.Handle(request, CancellationToken.None);

        Assert.Equal(date, result.Date);
    }

    [Fact]
    public async Task CreateAppointment_WithPatientUser_DoesNotCallMediator()
    {
        var doctor = BuildDoctor();
        var slot = BuildSlot(doctor.Id);
        var date = NextDateFor(slot.DayOfWeek);

        _doctorRepository.Setup(x => x.GetByIdAsync(doctor.Id, It.IsAny<CancellationToken>())).ReturnsAsync(doctor);
        _slotRepository.Setup(x => x.GetByIdAsync(slot.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slot);
        _patientRepository.Setup(x => x.GetByUserIdAsync("patient-5", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegisteredPatient("patient-5", "Nora"));
        _appointmentRepository.Setup(x => x.ExistsBySlotAndDateAsync(slot.Id, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new CreateAppointmentCommand(doctor.Id, slot.Id, date, "patient-5", null);
        await _sut.Handle(request, CancellationToken.None);

        _mediator.Verify(x => x.Send(It.IsAny<CreateGuestPatientCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Doctor BuildDoctor() => new("doctor-1", DoctorType.NaturalMedicine, "LIC-1", "");

    private static DoctorAvailabilitySlot BuildSlot(string doctorId, DayOfWeek day = DayOfWeek.Monday)
        => new(doctorId, day, TimeSpan.FromHours(9), TimeSpan.FromHours(10));

    private static DateOnly NextDateFor(DayOfWeek day)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1));
        while (date.DayOfWeek != day)
            date = date.AddDays(1);
        return date;
    }

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default)
            => action(ct);
    }
}
