using Moq;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Features.Appointments.CancelAppointment;
using PiedraAzul.Domain.Common.Exceptions;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Entities.Shared.Enums;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Test.Tests;

public class CancelAppointmentHandlerTests
{
    private readonly Mock<IAppointmentRepository> _appointmentRepository = new();
    private readonly Mock<IAppointmentNotifier> _notifier = new();

    private readonly CancelAppointmentHandler _sut;

    public CancelAppointmentHandlerTests()
    {
        _sut = new CancelAppointmentHandler(
            _appointmentRepository.Object,
            new ImmediateUnitOfWork(),
            _notifier.Object);
    }

    [Fact]
    public async Task CancelAppointment_WithValidData_ReturnsTrue()
    {
        var appointment = BuildActiveAppointment("user-1");

        _appointmentRepository
            .Setup(x => x.GetByIdForUpdateAsync(appointment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);

        var result = await _sut.Handle(new CancelAppointmentCommand(appointment.Id, "user-1"), CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task CancelAppointment_ChangesStatusToCancelled()
    {
        var appointment = BuildActiveAppointment("user-1");

        _appointmentRepository
            .Setup(x => x.GetByIdForUpdateAsync(appointment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);

        await _sut.Handle(new CancelAppointmentCommand(appointment.Id, "user-1"), CancellationToken.None);

        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
    }

    [Fact]
    public async Task CancelAppointment_WhenAppointmentNotFound_ThrowsDomainException()
    {
        var id = Guid.NewGuid();
        _appointmentRepository
            .Setup(x => x.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment?)null);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _sut.Handle(new CancelAppointmentCommand(id, "user-1"), CancellationToken.None).AsTask());

        Assert.Equal("Cita no encontrada.", ex.Message);
    }

    [Fact]
    public async Task CancelAppointment_WhenDifferentUser_ThrowsDomainException()
    {
        var appointment = BuildActiveAppointment("user-owner");

        _appointmentRepository
            .Setup(x => x.GetByIdForUpdateAsync(appointment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _sut.Handle(new CancelAppointmentCommand(appointment.Id, "user-other"), CancellationToken.None).AsTask());

        Assert.Equal("No tienes permiso para cancelar esta cita.", ex.Message);
    }

    [Fact]
    public async Task CancelAppointment_WhenAlreadyCancelled_ThrowsDomainException()
    {
        var appointment = BuildActiveAppointment("user-1");
        appointment.Cancel();

        _appointmentRepository
            .Setup(x => x.GetByIdForUpdateAsync(appointment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _sut.Handle(new CancelAppointmentCommand(appointment.Id, "user-1"), CancellationToken.None).AsTask());

        Assert.Equal("Esta cita ya fue cancelada o reagendada.", ex.Message);
    }

    [Fact]
    public async Task CancelAppointment_WhenAlreadyRescheduled_ThrowsDomainException()
    {
        var appointment = BuildActiveAppointment("user-1");
        appointment.MarkAsRescheduled(Guid.NewGuid());

        _appointmentRepository
            .Setup(x => x.GetByIdForUpdateAsync(appointment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _sut.Handle(new CancelAppointmentCommand(appointment.Id, "user-1"), CancellationToken.None).AsTask());

        Assert.Equal("Esta cita ya fue cancelada o reagendada.", ex.Message);
    }

    [Fact]
    public async Task CancelAppointment_CallsUpdateAsync()
    {
        var appointment = BuildActiveAppointment("user-1");

        _appointmentRepository
            .Setup(x => x.GetByIdForUpdateAsync(appointment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);

        await _sut.Handle(new CancelAppointmentCommand(appointment.Id, "user-1"), CancellationToken.None);

        _appointmentRepository.Verify(x => x.UpdateAsync(appointment, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelAppointment_NotifiesSlotReleased()
    {
        var appointment = BuildActiveAppointment("user-1");

        _appointmentRepository
            .Setup(x => x.GetByIdForUpdateAsync(appointment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);

        await _sut.Handle(new CancelAppointmentCommand(appointment.Id, "user-1"), CancellationToken.None);

        _notifier.Verify(x => x.NotifySlotReleasedAsync(
            appointment.DoctorId,
            appointment.DoctorAvailabilitySlotId.ToString(),
            It.IsAny<DateTime>()),
            Times.Once);
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
