using Mediator;
using PiedraAzul.Application.Features.Patients.Commands.CreateGuestPatient;
using PiedraAzul.Domain.Entities.Operations;
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

    public CreateAppointmentHandler(
        IAppointmentRepository appointmentRepository,
        IDoctorRepository doctorRepository,
        IDoctorAvailabilitySlotRepository slotRepository,
        IPatientRepository patientRepository,
        IPatientGuestRepository patientGuestRepository,
        IUnitOfWork unitOfWork,
        IMediator mediator)
    {
        _appointmentRepository = appointmentRepository;
        _doctorRepository = doctorRepository;
        _slotRepository = slotRepository;
        _patientRepository = patientRepository;
        _patientGuestRepository = patientGuestRepository;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    public async ValueTask<Appointment> Handle(
        CreateAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteAsync(async ct =>
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
                    throw new Exception("Patient not found");

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
                            guestRequest.ExtraInfo
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
    }
}