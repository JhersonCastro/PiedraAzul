using Moq;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Features.Appointments.RescheduleAppointment;
using PiedraAzul.Domain.Common.Exceptions;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Entities.Shared.Enums;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Test.Tests;

public class RescheduleAppointmentHandlerTests
{
    private readonly Mock<IAppointmentRepository> _appointmentRepository = new();
    private readonly Mock<IDoctorAvailabilitySlotRepository> _slotRepository = new();
    private readonly Mock<IAppointmentNotifier> _notifier = new();

    private readonly RescheduleAppointmentHandler _sut;

    public RescheduleAppointmentHandlerTests()
    {
        _sut = new RescheduleAppointmentHandler(
            _appointmentRepository.Object,
            _slotRepository.Object,
            new ImmediateUnitOfWork(),
            _notifier.Object);
    }

    [Fact]
    public async Task RescheduleAppointment_WithValidData_ReturnsNewAppointment()
    {
        var (old, newSlot, newDate) = SetupValidReschedule("user-1");

        var result = await _sut.Handle(
            new RescheduleAppointmentCommand(old.Id, "user-1", newSlot.Id, newDate),
            CancellationToken.None);

        Assert.NotEqual(old.Id, result.Id);
        Assert.Equal(newDate, result.Date);
    }

    [Fact]
    public async Task RescheduleAppointment_MarksOldAsRescheduled()
    {
        var (old, newSlot, newDate) = SetupValidReschedule("user-1");

        var result = await _sut.Handle(
            new RescheduleAppointmentCommand(old.Id, "user-1", newSlot.Id, newDate),
            CancellationToken.None);

        Assert.Equal(AppointmentStatus.Rescheduled, old.Status);
        Assert.Equal(result.Id, old.RescheduledToId);
    }

    [Fact]
    public async Task RescheduleAppointment_WhenNotFound_ThrowsDomainException()
    {
        var id = Guid.NewGuid();
        _appointmentRepository
            .Setup(x => x.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment?)null);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _sut.Handle(new RescheduleAppointmentCommand(id, "user-1", Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))), CancellationToken.None).AsTask());

        Assert.Equal("Cita no encontrada.", ex.Message);
    }

    [Fact]
    public async Task RescheduleAppointment_WhenDifferentUser_ThrowsDomainException()
    {
        var old = BuildActiveAppointment("owner-user");
        var newSlot = new DoctorAvailabilitySlot("doctor-1", DayOfWeek.Wednesday, TimeSpan.FromHours(10), TimeSpan.FromHours(11));
        var newDate = NextDateFor(DayOfWeek.Wednesday);

        _appointmentRepository
            .Setup(x => x.GetByIdForUpdateAsync(old.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(old);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _sut.Handle(new RescheduleAppointmentCommand(old.Id, "other-user", newSlot.Id, newDate), CancellationToken.None).AsTask());

        Assert.Equal("No tienes permiso para reagendar esta cita.", ex.Message);
    }

    [Fact]
    public async Task RescheduleAppointment_WhenAlreadyCancelled_ThrowsDomainException()
    {
        var old = BuildActiveAppointment("user-1");
        old.Cancel();

        _appointmentRepository
            .Setup(x => x.GetByIdForUpdateAsync(old.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(old);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _sut.Handle(new RescheduleAppointmentCommand(old.Id, "user-1", Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))), CancellationToken.None).AsTask());

        Assert.Equal("Esta cita ya fue cancelada o reagendada.", ex.Message);
    }

    [Fact]
    public async Task RescheduleAppointment_WhenNewSlotNotFound_ThrowsDomainException()
    {
        var old = BuildActiveAppointment("user-1");
        var newSlotId = Guid.NewGuid();

        _appointmentRepository
            .Setup(x => x.GetByIdForUpdateAsync(old.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(old);
        _slotRepository
            .Setup(x => x.GetByIdAsync(newSlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DoctorAvailabilitySlot?)null);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _sut.Handle(new RescheduleAppointmentCommand(old.Id, "user-1", newSlotId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))), CancellationToken.None).AsTask());

        Assert.Equal("El horario seleccionado no existe.", ex.Message);
    }

    [Fact]
    public async Task RescheduleAppointment_WhenNewSlotTaken_ThrowsDomainException()
    {
        var old = BuildActiveAppointment("user-1");
        var newSlot = new DoctorAvailabilitySlot("doctor-1", DayOfWeek.Wednesday, TimeSpan.FromHours(10), TimeSpan.FromHours(11));
        var newDate = NextDateFor(DayOfWeek.Wednesday);

        _appointmentRepository
            .Setup(x => x.GetByIdForUpdateAsync(old.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(old);
        _slotRepository
            .Setup(x => x.GetByIdAsync(newSlot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newSlot);
        _appointmentRepository
            .Setup(x => x.ExistsBySlotAndDateAsync(newSlot.Id, newDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _sut.Handle(new RescheduleAppointmentCommand(old.Id, "user-1", newSlot.Id, newDate), CancellationToken.None).AsTask());

        Assert.Equal("El horario seleccionado ya está ocupado.", ex.Message);
    }

    [Fact]
    public async Task RescheduleAppointment_NotifiesOldSlotReleased()
    {
        var (old, newSlot, newDate) = SetupValidReschedule("user-1");

        await _sut.Handle(
            new RescheduleAppointmentCommand(old.Id, "user-1", newSlot.Id, newDate),
            CancellationToken.None);

        _notifier.Verify(x => x.NotifySlotReleasedAsync(
            old.DoctorId,
            old.DoctorAvailabilitySlotId.ToString(),
            It.IsAny<DateTime>()),
            Times.Once);
    }

    [Fact]
    public async Task RescheduleAppointment_AddsNewAndUpdatesOld()
    {
        var (old, newSlot, newDate) = SetupValidReschedule("user-1");

        await _sut.Handle(
            new RescheduleAppointmentCommand(old.Id, "user-1", newSlot.Id, newDate),
            CancellationToken.None);

        _appointmentRepository.Verify(x => x.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Once);
        _appointmentRepository.Verify(x => x.UpdateAsync(old, It.IsAny<CancellationToken>()), Times.Once);
    }

    private (Appointment old, DoctorAvailabilitySlot newSlot, DateOnly newDate) SetupValidReschedule(string userId)
    {
        var old = BuildActiveAppointment(userId);
        var newSlot = new DoctorAvailabilitySlot("doctor-1", DayOfWeek.Wednesday, TimeSpan.FromHours(10), TimeSpan.FromHours(11));
        var newDate = NextDateFor(DayOfWeek.Wednesday);

        _appointmentRepository
            .Setup(x => x.GetByIdForUpdateAsync(old.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(old);
        _slotRepository
            .Setup(x => x.GetByIdAsync(newSlot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newSlot);
        _appointmentRepository
            .Setup(x => x.ExistsBySlotAndDateAsync(newSlot.Id, newDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        return (old, newSlot, newDate);
    }

    private static Appointment BuildActiveAppointment(string userId)
    {
        var slot = new DoctorAvailabilitySlot("doctor-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        var date = NextDateFor(DayOfWeek.Monday);
        return Appointment.Create(slot, date, "doctor-1", userId, null);
    }

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
