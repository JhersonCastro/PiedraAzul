using Mediator;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Domain.Common.Exceptions;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Application.Features.Appointments.RescheduleAppointment;

public class RescheduleAppointmentByHashHandler
    : IRequestHandler<RescheduleAppointmentByHashCommand, Appointment>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IDoctorAvailabilitySlotRepository _slotRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGuestOtpService _guestOtpService;
    private readonly IAppointmentNotifier _notifier;

    public RescheduleAppointmentByHashHandler(
        IAppointmentRepository appointmentRepository,
        IDoctorAvailabilitySlotRepository slotRepository,
        IUnitOfWork unitOfWork,
        IGuestOtpService guestOtpService,
        IAppointmentNotifier notifier)
    {
        _appointmentRepository = appointmentRepository;
        _slotRepository = slotRepository;
        _unitOfWork = unitOfWork;
        _guestOtpService = guestOtpService;
        _notifier = notifier;
    }

    public async ValueTask<Appointment> Handle(
        RescheduleAppointmentByHashCommand request,
        CancellationToken ct)
    {
        // 1. Verificar que el hash está verificado
        var verified = await _guestOtpService.IsSessionVerifiedAsync(request.VerificationHash);
        if (!verified)
            throw new DomainException("La sesión de verificación no es válida o expiró.");

        // 2. Obtener datos del guest (para validar que la cita le pertenece)
        var sessionData = await _guestOtpService.GetVerifiedDataAsync(request.VerificationHash)
            ?? throw new DomainException("No se pudieron obtener los datos de verificación.");

        return await _unitOfWork.ExecuteAsync(async ct =>
        {
            // 3. Cargar la cita con tracking
            var old = await _appointmentRepository.GetByIdForUpdateAsync(request.AppointmentId, ct)
                ?? throw new DomainException("Cita no encontrada.");

            // 4. Validar que la cita pertenece al guest verificado
            var belongsToGuest = old.PatientGuestId == sessionData.Id;
            var belongsToRegistered = old.PatientUserId == sessionData.Id;

            if (!belongsToGuest && !belongsToRegistered)
                throw new DomainException("No tienes permiso para reagendar esta cita.");

            if (old.Status != AppointmentStatus.Active)
                throw new DomainException("Esta cita ya fue cancelada o reagendada.");

            // 5. Cargar nuevo slot
            var newSlot = await _slotRepository.GetByIdAsync(request.NewSlotId, ct)
                ?? throw new DomainException("El horario seleccionado no existe.");

            // 6. Verificar disponibilidad
            var taken = await _appointmentRepository.ExistsBySlotAndDateAsync(request.NewSlotId, request.NewDate, ct);
            if (taken)
                throw new DomainException("El horario seleccionado ya está ocupado.");

            // 7. Crear nueva cita — el doctor se deriva del slot nuevo
            //    (puede ser distinto del doctor original si el usuario eligió otro médico).
            //    Mantiene el mismo tipo de paciente que la vieja.
            var newAppt = Appointment.Create(
                newSlot,
                request.NewDate,
                newSlot.DoctorId,
                old.PatientUserId,
                old.PatientGuestId);

            // 8. Soft-delete de la vieja
            var oldSlotId = old.DoctorAvailabilitySlotId;
            var oldDate = old.Date;
            var oldDoctorId = old.DoctorId;
            old.MarkAsRescheduled(newAppt.Id);

            // 9. Persistir
            await _appointmentRepository.AddAsync(newAppt, ct);
            await _appointmentRepository.UpdateAsync(old, ct);

            // 10. Notificar via SignalR que el slot viejo quedó libre
            await _notifier.NotifySlotReleasedAsync(
                oldDoctorId,
                oldSlotId.ToString(),
                oldDate.ToDateTime(TimeOnly.MinValue));

            return newAppt;
        }, ct);
    }
}
