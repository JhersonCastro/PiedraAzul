using Mediator;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Domain.Common.Exceptions;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Application.Features.Appointments.CancelAppointment;

public class CancelAppointmentHandler : IRequestHandler<CancelAppointmentCommand, bool>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppointmentNotifier _notifier;

    public CancelAppointmentHandler(
        IAppointmentRepository appointmentRepository,
        IUnitOfWork unitOfWork,
        IAppointmentNotifier notifier)
    {
        _appointmentRepository = appointmentRepository;
        _unitOfWork = unitOfWork;
        _notifier = notifier;
    }

    public async ValueTask<bool> Handle(CancelAppointmentCommand request, CancellationToken ct)
    {
        return await _unitOfWork.ExecuteAsync(async ct =>
        {
            var appointment = await _appointmentRepository.GetByIdForUpdateAsync(request.AppointmentId, ct)
                ?? throw new DomainException("Cita no encontrada.");

            if (appointment.PatientUserId != request.RequestingUserId)
                throw new DomainException("No tienes permiso para cancelar esta cita.");

            if (appointment.Status != AppointmentStatus.Active)
                throw new DomainException("Esta cita ya fue cancelada o reagendada.");

            var slotId    = appointment.DoctorAvailabilitySlotId;
            var date      = appointment.Date;
            var doctorId  = appointment.DoctorId;

            appointment.Cancel();
            await _appointmentRepository.UpdateAsync(appointment, ct);

            // Liberar el slot en SignalR para que otros pacientes puedan verlo disponible
            await _notifier.NotifySlotReleasedAsync(
                doctorId,
                slotId.ToString(),
                date.ToDateTime(TimeOnly.MinValue));

            return true;
        }, ct);
    }
}
