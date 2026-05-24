using Mediator;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.Appointments;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Application.Features.Patients.Queries.GetPatientAppointments
{
    public sealed class GetPatientAppointmentsHandler
        : IRequestHandler<GetPatientAppointmentsQuery, IReadOnlyList<AppointmentDto>>
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IPatientGuestRepository _patientGuestRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IIdentityService _identityService;
        private readonly IDoctorRepository _doctorRepository;
        private readonly IDoctorAvailabilitySlotRepository _slotRepository;

        public GetPatientAppointmentsHandler(
            IPatientRepository patientRepository,
            IPatientGuestRepository patientGuestRepository,
            IAppointmentRepository appointmentRepository,
            IIdentityService identityService,
            IDoctorRepository doctorRepository,
            IDoctorAvailabilitySlotRepository slotRepository)
        {
            _patientRepository = patientRepository;
            _patientGuestRepository = patientGuestRepository;
            _appointmentRepository = appointmentRepository;
            _identityService = identityService;
            _doctorRepository = doctorRepository;
            _slotRepository = slotRepository;
        }

        public async ValueTask<IReadOnlyList<AppointmentDto>> Handle(
            GetPatientAppointmentsQuery request,
            CancellationToken cancellationToken)
        {
            if (request.PatientUserId is null && request.PatientGuestId is null)
                throw new ArgumentException("Either patientUserId or patientGuestId must be provided.");

            if (request.PatientUserId is not null && request.PatientGuestId is not null)
                throw new ArgumentException("Only one patient identifier must be provided.");

            // ✅ Tipo correcto
            IReadOnlyList<Appointment> appointments;

            if (request.PatientUserId is not null)
            {
                appointments = await _appointmentRepository.ListByPatientUserAsync(
                    request.PatientUserId,
                    request.Date,
                    cancellationToken);
            }
            else
            {
                appointments = await _appointmentRepository.ListByPatientGuestAsync(
                    request.PatientGuestId!,
                    request.Date,
                    cancellationToken);
            }

            if (appointments.Count == 0)
                return [];

            // 🔥 Obtener usuarios registrados (pacientes)
            var userIds = appointments
                .Where(a => a.PatientUserId != null)
                .Select(a => a.PatientUserId!)
                .Distinct()
                .ToList();

            var users = await _identityService.GetByIds(userIds);
            var userDict = users.ToDictionary(u => u.Id);

            // 🔥 Obtener invitados
            var guestIds = appointments
                .Where(a => a.PatientGuestId != null)
                .Select(a => a.PatientGuestId!)
                .Distinct()
                .ToList();

            var guests = await _patientGuestRepository.GetByIdsAsync(guestIds, cancellationToken);
            var guestDict = guests.ToDictionary(g => g.Id);

            // 🔥 Obtener nombres de doctores (de identity) y especialidad (del repositorio)
            var doctorIds = appointments
                .Select(a => a.DoctorId)
                .Distinct()
                .ToList();

            var doctorUsers = await _identityService.GetByIds(doctorIds);
            var doctorUserDict = doctorUsers.ToDictionary(u => u.Id);

            // ✅ CORRECCIÓN: ejecución secuencial en lugar de Task.WhenAll para evitar concurrencia en DbContext
            var doctorEntities = new List<Doctor>();
            foreach (var id in doctorIds)
            {
                var doctor = await _doctorRepository.GetByIdAsync(id, cancellationToken);
                if (doctor is not null)
                    doctorEntities.Add(doctor);
            }
            var doctorEntityDict = doctorEntities.ToDictionary(d => d.Id);

            // 🔥 Obtener hora real del slot (StartTime)
            var slotIds = appointments
                .Select(a => a.DoctorAvailabilitySlotId)
                .Distinct()
                .ToList();

            var slots = await _slotRepository.GetByIdsAsync(slotIds, cancellationToken);
            var slotDict = slots.ToDictionary(s => s.Id);

            // 🔥 Mapping a DTO
            return appointments.Select(a =>
            {
                string name;
                string type;

                if (a.PatientUserId != null)
                {
                    var user = userDict[a.PatientUserId];
                    name = user.Name;
                    type = "Registered";
                }
                else
                {
                    var guest = guestDict[a.PatientGuestId!];
                    name = guest.Name;
                    type = "Guest";
                }

                doctorUserDict.TryGetValue(a.DoctorId, out var doctorUser);
                doctorEntityDict.TryGetValue(a.DoctorId, out var doctorEntity);

                // Combinar fecha del appointment + hora real del slot
                slotDict.TryGetValue(a.DoctorAvailabilitySlotId, out var slot);
                var startTime = slot is not null
                    ? TimeOnly.FromTimeSpan(slot.StartTime)
                    : TimeOnly.MinValue;
                var start = a.Date.ToDateTime(startTime);

                return new AppointmentDto
                {
                    Id = a.Id,
                    PatientUserId = a.PatientUserId,
                    PatientGuestId = a.PatientGuestId,
                    PatientName = name,
                    PatientType = type,
                    SlotId = a.DoctorAvailabilitySlotId,
                    DoctorId = a.DoctorId,
                    DoctorName = doctorUser?.Name ?? "",
                    Specialty = doctorEntity?.Specialty.ToString() ?? "",
                    Start = start,
                    CreatedAt = a.CreatedAt,
                    Status = a.Status.ToString()
                };
            }).ToList();
        }
    }
}