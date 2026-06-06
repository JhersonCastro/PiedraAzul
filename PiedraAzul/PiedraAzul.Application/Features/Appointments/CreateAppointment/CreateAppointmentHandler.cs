using Mediator;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Notifications;
using PiedraAzul.Application.Features.Patients.Commands.CreateGuestPatient;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Entities.Profiles.Patients;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Application.Features.Appointments.CreateAppointment;

public class CreateAppointmentHandler
    : IRequestHandler<CreateAppointmentCommand, Appointment>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IDoctorAvailabilitySlotRepository _slotRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IPatientGuestRepository _patientGuestRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly IIdentityService _identityService;
    private readonly IAppointmentBackgroundJobsRecordsRepository _appointmentBackgroundJobsRecordsRepository;
    private readonly IBackgroundNotificationService _backgroundNotificationService;

    public CreateAppointmentHandler(
        IAppointmentRepository appointmentRepository,
        IDoctorRepository doctorRepository,
        IDoctorAvailabilitySlotRepository slotRepository,
        IPatientRepository patientRepository,
        IPatientGuestRepository patientGuestRepository,
        IUnitOfWork unitOfWork,
        IMediator mediator,
        IIdentityService identityService,
        IAppointmentBackgroundJobsRecordsRepository appointmentBackgroundJobsRecordsRepository,
        IBackgroundNotificationService backgroundNotificationService)
    {
        _appointmentRepository = appointmentRepository;
        _doctorRepository = doctorRepository;
        _slotRepository = slotRepository;
        _patientRepository = patientRepository;
        _patientGuestRepository = patientGuestRepository;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
        _identityService = identityService;
        _appointmentBackgroundJobsRecordsRepository = appointmentBackgroundJobsRecordsRepository;
        _backgroundNotificationService = backgroundNotificationService;
    }

    public async ValueTask<Appointment> Handle(
        CreateAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var appointment = await _unitOfWork.ExecuteAsync(async ct =>
        {
            // ================= VALIDACIONES =================

            var doctor = await _doctorRepository
                .GetByIdAsync(request.DoctorId, ct);

            if (doctor is null)
                throw new Exception("Doctor not found");

            var slot = await _slotRepository
                .GetByIdAsync(request.SlotId, ct);

            if (slot is null)
                throw new Exception("Slot not found");

            string? userId = null;
            string? guestId = null;

            // ================= PACIENTE =================

            if (request.PatientUserId is not null)
            {
                var patient = await _patientRepository
                    .GetByUserIdAsync(request.PatientUserId, ct);

                if (patient is null)
                {
                    // Auto-crear el RegisteredPatient si aún no existe en la tabla Patients.
                    // Esto cubre usuarios legacy o creados por admin que no pasaron por el
                    // flujo de registro estándar (CreateProfileForRoleAsync).
                    var userDto = await _identityService.GetById(request.PatientUserId);
                    var name = userDto?.Name ?? userDto?.Email ?? string.Empty;
                    await _patientRepository.AddAsync(new RegisteredPatient(request.PatientUserId, name), ct);
                }

                userId = request.PatientUserId;
            }
            else if (request.PatientGuest is not null)
            {
                var guestRequest = request.PatientGuest;

                var guest = await _patientGuestRepository
                    .GetByIdAsync(guestRequest.Identification, ct);

                if (guest is null)
                {
                    var newGuest = await _mediator.Send(
                        new CreateGuestPatientCommand(
                            guestRequest.Identification,
                            guestRequest.Name,
                            guestRequest.Phone,
                            guestRequest.ExtraInfo,
                            guestRequest.Email,
                            guestRequest.DocumentType
                        ),
                        ct
                    );

                    if (newGuest is null)
                        throw new Exception("Failed to create guest patient");

                    guestId = newGuest;
                }
                else
                {
                    guestId = guest.Id; // ✅ usar ID existente
                }
            }
            else
            {
                throw new Exception("Patient required");
            }

            // ================= VALIDAR SLOT =================

            var exists = await _appointmentRepository
                .ExistsBySlotAndDateAsync(
                    request.SlotId,
                    request.Date,
                    ct);

            if (exists)
                throw new Exception("Slot already taken");

            // ================= CREAR APPOINTMENT =================

            var appointment = Appointment.Create(
                slot,
                request.Date,
                request.DoctorId,
                userId,
                guestId
            );

            await _appointmentRepository.AddAsync(appointment, ct);

            return appointment;

        }, cancellationToken);

        // Notificar por email fuera de la transacción (no debe afectar el guardado).
        await _mediator.Publish(
            new AppointmentNotification(
                AppointmentChange.Created,
                appointment.PatientUserId,
                appointment.PatientGuestId,
                appointment.DoctorId,
                appointment.DoctorAvailabilitySlotId,
                appointment.Date),
            cancellationToken);


        // Enviar notificacion programada para recordatorio (24h antes) en el background job service (Hangfire).

        _ = await _unitOfWork.ExecuteAsync(async ct =>
        {
            // El slot ya está cargado en este scope → calculamos appointmentStart aquí.
            var slot = await _slotRepository.GetByIdAsync(appointment.DoctorAvailabilitySlotId, ct);
            var appointmentStart = appointment.Date.ToDateTime(TimeOnly.FromTimeSpan(slot!.StartTime));

            // Recordatorio 24 h antes y 1 h antes.
            TimeSpan[] reminders =
            [
                TimeSpan.FromHours(24),
                TimeSpan.FromHours(1)
            ];
            var jobsId = await _backgroundNotificationService.ScheduleAppointmentNotification(appointment.Id, appointmentStart, reminders);

            List<AppointmentBackgroundJobsRecords> records = new List<AppointmentBackgroundJobsRecords>();
            records.AddRange(jobsId.Select(id => new AppointmentBackgroundJobsRecords
            {
                AppointmentId = appointment.Id,
                JobId = id
            }));
            await _appointmentBackgroundJobsRecordsRepository.AddJobsRecords(records, ct);

            return true;
        }, cancellationToken);

        return appointment;
    }
}