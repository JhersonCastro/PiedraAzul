using Mediator;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Domain.Common.Exceptions;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Application.Features.Appointments.UpdateAppointmentStatus;

public class UpdateAppointmentStatusHandler(
    IAppointmentRepository appointmentRepository,
    IDoctorAvailabilitySlotRepository slotRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateAppointmentStatusCommand, bool>
{
    public async ValueTask<bool> Handle(
        UpdateAppointmentStatusCommand request,
        CancellationToken cancellationToken)
    {
        // Solo Completed o NoShow son estados válidos para este comando
        if (request.NewStatus != AppointmentStatus.Completed &&
            request.NewStatus != AppointmentStatus.NoShow)
            throw new DomainException("Solo se puede marcar una cita como Completada o Inasistencia.");

        var appointment = await appointmentRepository
            .GetByIdForUpdateAsync(request.AppointmentId, cancellationToken)
            ?? throw new DomainException("Cita no encontrada.");

        // El doctor solo puede actualizar sus propias citas
        if (appointment.DoctorId != request.DoctorUserId)
            throw new DomainException("No tienes permiso para modificar esta cita.");

        // ── Validar que la cita ya haya comenzado ────────────────────────────────
        // slot.StartTime es hora Colombia local (UTC-5, sin DST).
        // Convertimos UtcNow a Colombia para comparar en el mismo marco de referencia.
        var slot = await slotRepository.GetByIdAsync(appointment.DoctorAvailabilitySlotId, cancellationToken);
        if (slot is not null)
        {
            var appointmentStartColombia = appointment.Date.ToDateTime(TimeOnly.MinValue)
                .Add(slot.StartTime);
            var nowColombia = DateTime.UtcNow.AddHours(-5);

            if (appointmentStartColombia > nowColombia)
                throw new DomainException(
                    $"No puedes marcar esta cita: todavía no ha llegado su hora " +
                    $"({slot.StartTime:hh\\:mm} hora Colombia). " +
                    $"Inténtalo una vez que haya comenzado.");
        }

        if (request.NewStatus == AppointmentStatus.Completed)
            appointment.Complete();
        else
            appointment.MarkAsNoShow();

        await unitOfWork.ExecuteAsync(async ct =>
        {
            await appointmentRepository.UpdateAsync(appointment, ct);
            return true;
        }, cancellationToken);

        return true;
    }
}
