using Mediator;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Notifications;
using PiedraAzul.Domain.Common.Exceptions;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Application.Features.Appointments.RescheduleAppointment;

public class RescheduleAppointmentHandler
    : IRequestHandler<RescheduleAppointmentCommand, Appointment>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IDoctorAvailabilitySlotRepository _slotRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppointmentNotifier _notifier;
    private readonly IAppointmentRescheduleRecordRepository _rescheduleRecordRepository;
    private readonly IIdentityService _identityService;
    private readonly IMediator _mediator;

    public RescheduleAppointmentHandler(
        IAppointmentRepository appointmentRepository,
        IDoctorAvailabilitySlotRepository slotRepository,
        IUnitOfWork unitOfWork,
        IAppointmentNotifier notifier,
        IAppointmentRescheduleRecordRepository rescheduleRecordRepository,
        IIdentityService identityService,
        IMediator mediator)
    {
        _appointmentRepository = appointmentRepository;
        _slotRepository = slotRepository;
        _unitOfWork = unitOfWork;
        _notifier = notifier;
        _rescheduleRecordRepository = rescheduleRecordRepository;
        _identityService = identityService;
        _mediator = mediator;
    }

    public async ValueTask<Appointment> Handle(
        RescheduleAppointmentCommand request,
        CancellationToken ct)
    {
        var createdAppt = await _unitOfWork.ExecuteAsync(async ct =>
        {
            // 1. Cargar la cita con tracking
            var old = await _appointmentRepository.GetByIdForUpdateAsync(request.AppointmentId, ct)
                ?? throw new DomainException("Cita no encontrada.");

            // 2. Permisos derivados del id del solicitante:
            //    paciente dueño, doctor de la cita, o administrador.
            var roles = await _identityService.GetRolesByUser(request.RequestingUserId);
            var isAdmin = roles.Contains("Admin");
            var isDoctorOfAppointment = old.DoctorId == request.RequestingUserId;
            var isOwnerPatient = old.PatientUserId == request.RequestingUserId;

            if (!isAdmin && !isDoctorOfAppointment && !isOwnerPatient)
                throw new DomainException("No tienes permiso para reagendar esta cita.");

            if (old.Status != AppointmentStatus.Active)
                throw new DomainException("Esta cita ya fue cancelada o reagendada.");

            // 3. Cargar nuevo slot
            var newSlot = await _slotRepository.GetByIdAsync(request.NewSlotId, ct)
                ?? throw new DomainException("El horario seleccionado no existe.");

            // 4. Verificar disponibilidad del nuevo slot
            var taken = await _appointmentRepository.ExistsBySlotAndDateAsync(request.NewSlotId, request.NewDate, ct);
            if (taken)
                throw new DomainException("El horario seleccionado ya está ocupado.");

            // 5. Crear nueva cita — el doctor se deriva del slot nuevo
            //    (puede ser distinto del doctor original si el usuario eligió otro médico).
            var newAppt = Appointment.Create(
                newSlot,
                request.NewDate,
                newSlot.DoctorId,
                old.PatientUserId,
                null);

            // 6. Soft-delete de la vieja
            var oldSlotId = old.DoctorAvailabilitySlotId;
            var oldDate = old.Date;
            var oldDoctorId = old.DoctorId;
            old.MarkAsRescheduled(newAppt.Id);

            // 7. Registro de auditoría. Calcular el linaje (Root) buscando si la vieja
            //    ya provenía de un reagendamiento anterior.
            var previous = await _rescheduleRecordRepository.GetByNewAppointmentIdAsync(old.Id, ct);
            var rootAppointmentId = previous?.RootAppointmentId ?? old.Id;

            var record = AppointmentRescheduleRecord.Create(
                rootAppointmentId,
                old.Id,
                newAppt.Id,
                request.RequestingUserId,
                oldDate,
                request.NewDate,
                oldDoctorId,
                newSlot.DoctorId);

            // 8. Persistir
            await _appointmentRepository.AddAsync(newAppt, ct);
            await _appointmentRepository.UpdateAsync(old, ct);
            await _rescheduleRecordRepository.AddAsync(record, ct);

            // 9. Notificar via SignalR que el slot viejo quedó libre
            await _notifier.NotifySlotReleasedAsync(
                oldDoctorId,
                oldSlotId.ToString(),
                oldDate.ToDateTime(TimeOnly.MinValue));

            return newAppt;
        }, ct);

        // Notificar por email fuera de la transacción.
        await _mediator.Publish(
            new AppointmentNotification(
                AppointmentChange.Rescheduled,
                createdAppt.PatientUserId,
                createdAppt.PatientGuestId,
                createdAppt.DoctorId,
                createdAppt.DoctorAvailabilitySlotId,
                createdAppt.Date),
            ct);

        return createdAppt;
    }
}
