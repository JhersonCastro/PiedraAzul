using Mediator;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.Appointments;
using PiedraAzul.Domain.Entities.Operations;
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

        public GetPatientAppointmentsHandler(
            IPatientRepository patientRepository,
            IPatientGuestRepository patientGuestRepository,
            IAppointmentRepository appointmentRepository,
            IIdentityService identityService)
        {
            _patientRepository = patientRepository;
            _patientGuestRepository = patientGuestRepository;
            _appointmentRepository = appointmentRepository;
            _identityService = identityService;
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

            // 🔥 Obtener usuarios registrados
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

                var start = a.Date.ToDateTime(TimeOnly.MinValue);

                return new AppointmentDto
                {
                    Id = a.Id,
                    PatientUserId = a.PatientUserId,
                    PatientGuestId = a.PatientGuestId,
                    PatientName = name,
                    PatientType = type,
                    SlotId = a.DoctorAvailabilitySlotId,
                    Start = start,
                    CreatedAt = a.CreatedAt
                };
            }).ToList();
        }
    }
}