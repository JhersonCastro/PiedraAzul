using Mediator;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Notifications;
using PiedraAzul.Domain.Common.Exceptions;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Application.Features.Appointments.CancelAppointment;

public class CancelAppointmentHandler : IRequestHandler<CancelAppointmentCommand, bool>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppointmentNotifier _notifier;
    private readonly IMediator _mediator;
    private readonly IBackgroundNotificationService _backgroundNotificationService;
    private readonly IAppointmentBackgroundJobsRecordsRepository _jobsRecordsRepository;

    public CancelAppointmentHandler(
        IAppointmentRepository appointmentRepository,
        IUnitOfWork unitOfWork,
        IAppointmentNotifier notifier,
        IMediator mediator,
        IBackgroundNotificationService backgroundNotificationService,
        IAppointmentBackgroundJobsRecordsRepository jobsRecordsRepository)
    {
        _appointmentRepository = appointmentRepository;
        _unitOfWork = unitOfWork;
        _notifier = notifier;
        _mediator = mediator;
        _backgroundNotificationService = backgroundNotificationService;
        _jobsRecordsRepository = jobsRecordsRepository;
    }

    public async ValueTask<bool> Handle(CancelAppointmentCommand request, CancellationToken ct)
    {
        string? patientUserId = null;
        string? patientGuestId = null;
        string doctorId = "";
        Guid slotId = default;
        DateOnly date = default;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var appointment = await _appointmentRepository.GetByIdForUpdateAsync(request.AppointmentId, ct)
                ?? throw new DomainException("Cita no encontrada.");

            if (appointment.PatientUserId != request.RequestingUserId)
                throw new DomainException("No tienes permiso para cancelar esta cita.");

            if (appointment.Status != AppointmentStatus.Active)
                throw new DomainException("Esta cita ya fue cancelada o reagendada.");

            slotId         = appointment.DoctorAvailabilitySlotId;
            date           = appointment.Date;
            doctorId       = appointment.DoctorId;
            patientUserId  = appointment.PatientUserId;
            patientGuestId = appointment.PatientGuestId;

            appointment.Cancel();
            await _appointmentRepository.UpdateAsync(appointment, ct);

            // Liberar el slot en SignalR para que otros pacientes puedan verlo disponible
            await _notifier.NotifySlotReleasedAsync(
                doctorId,
                slotId.ToString(),
                date.ToDateTime(TimeOnly.MinValue));

            return true;
        }, ct);

        // Notificar por email fuera de la transacción.
        await _mediator.Publish(
            new AppointmentNotification(
                AppointmentChange.Cancelled,
                patientUserId,
                patientGuestId,
                doctorId,
                slotId,
                date),
            ct);
         
        // Cancelar notificaciones programadas en el background job service (Hangfire).
        var jobsToCancel = await _jobsRecordsRepository.GetJobsIdsByAppointmentId(request.AppointmentId, ct);

        await _backgroundNotificationService.CancelScheduleAppointmentNotification(jobsToCancel);

        return true;
    }
}
