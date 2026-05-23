using Mediator;
using PiedraAzul.Application.Common.Interfaces;
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

    public RescheduleAppointmentHandler(
        IAppointmentRepository appointmentRepository,
        IDoctorAvailabilitySlotRepository slotRepository,
        IUnitOfWork unitOfWork,
        IAppointmentNotifier notifier)
    {
        _appointmentRepository = appointmentRepository;
        _slotRepository = slotRepository;
        _unitOfWork = unitOfWork;
        _notifier = notifier;
    }

    public async ValueTask<Appointment> Handle(
        RescheduleAppointmentCommand request,
        CancellationToken ct)
    {
        return await _unitOfWork.ExecuteAsync(async ct =>
        {
            // 1. Cargar la cita con tracking
            var old = await _appointmentRepository.GetByIdForUpdateAsync(request.AppointmentId, ct)
                ?? throw new DomainException("Cita no encontrada.");

            // 2. Validar que pertenece al usuario que solicita
            if (old.PatientUserId != request.RequestingUserId)
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

            // 7. Persistir
            await _appointmentRepository.AddAsync(newAppt, ct);
            await _appointmentRepository.UpdateAsync(old, ct);

            // 8. Notificar via SignalR que el slot viejo quedó libre
            await _notifier.NotifySlotReleasedAsync(
                oldDoctorId,
                oldSlotId.ToString(),
                oldDate.ToDateTime(TimeOnly.MinValue));

            return newAppt;
        }, ct);
    }
}
