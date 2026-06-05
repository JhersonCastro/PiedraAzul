using Mediator;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.Patients;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Application.Features.Doctors.Queries.GetDoctorPatients;

public class GetDoctorPatientsHandler
    : IRequestHandler<GetDoctorPatientsQuery, IReadOnlyList<DoctorPatientDto>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IPatientGuestRepository _guestRepository;
    private readonly IIdentityService _identityService;

    public GetDoctorPatientsHandler(
        IAppointmentRepository appointmentRepository,
        IPatientGuestRepository guestRepository,
        IIdentityService identityService)
    {
        _appointmentRepository = appointmentRepository;
        _guestRepository = guestRepository;
        _identityService = identityService;
    }

    public async ValueTask<IReadOnlyList<DoctorPatientDto>> Handle(
        GetDoctorPatientsQuery request,
        CancellationToken cancellationToken)
    {
        // Todas las citas del doctor (Active, Completed, NoShow, Cancelled)
        var appointments = await _appointmentRepository.ListByDoctorAsync(request.DoctorId);

        var result = new List<DoctorPatientDto>();

        // --- Pacientes Registrados ---
        var registeredGroups = appointments
            .Where(a => a.PatientUserId != null)
            .GroupBy(a => a.PatientUserId!)
            .ToList();

        var registeredIds = registeredGroups.Select(g => g.Key).ToList();
        var users = registeredIds.Count > 0
            ? await _identityService.GetPatientUsersByIds(registeredIds)
            : [];

        var usersDict = users.ToDictionary(u => u.Id);

        foreach (var group in registeredGroups)
        {
            usersDict.TryGetValue(group.Key, out var user);
            var lastVisit = group.Max(a => a.Date);
            result.Add(new DoctorPatientDto
            {
                Id = group.Key,
                Name = user?.Name ?? group.Key,
                Identification = user?.Identification ?? "",
                Phone = user?.Phone ?? "",
                Type = PatientKind.Registered,
                LastVisit = lastVisit.ToDateTime(TimeOnly.MinValue)
            });
        }

        // --- Pacientes Invitados ---
        // Construimos un índice de identificaciones de usuarios registrados para deduplicar:
        // Si un invitado tiene la misma identificación que un registrado, es la misma persona
        // (ocurre cuando alguien con cuenta fue agendado como invitado por error).
        var registeredIdentifications = users
            .Where(u => !string.IsNullOrWhiteSpace(u.Identification))
            .Select(u => u.Identification.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // También un índice para actualizar la última visita si el invitado tiene fecha más reciente
        var registeredByIdentification = result
            .ToDictionary(r => r.Identification?.Trim() ?? "", StringComparer.OrdinalIgnoreCase);

        var guestGroups = appointments
            .Where(a => a.PatientGuestId != null)
            .GroupBy(a => a.PatientGuestId!)
            .ToList();

        var guestIds = guestGroups.Select(g => g.Key).ToList();
        var guests = guestIds.Count > 0
            ? await _guestRepository.GetByIdsAsync(guestIds, cancellationToken)
            : [];
        var guestsDict = guests.ToDictionary(g => g.Id);

        foreach (var group in guestGroups)
        {
            guestsDict.TryGetValue(group.Key, out var guest);
            var lastVisit = group.Max(a => a.Date);
            // La identificación del invitado es su PatientGuestId (que es su número de identificación)
            var identification = guest?.Id ?? group.Key;

            // Si esta identificación ya existe en los registrados → es el mismo paciente, fusionar
            if (registeredIdentifications.Contains(identification))
            {
                // Actualizar la última visita del registrado si la del invitado es más reciente
                if (registeredByIdentification.TryGetValue(identification, out var existingRegistered))
                {
                    var guestLastVisitDt = lastVisit.ToDateTime(TimeOnly.MinValue);
                    if (existingRegistered.LastVisit.HasValue && guestLastVisitDt > existingRegistered.LastVisit.Value)
                        existingRegistered.LastVisit = guestLastVisitDt;
                }
                // No agregar duplicado
                continue;
            }

            result.Add(new DoctorPatientDto
            {
                Id             = group.Key,
                Name           = guest?.Name ?? group.Key,
                Identification = identification,
                Phone          = guest?.Phone ?? "",
                Type           = PatientKind.Guest,
                LastVisit      = lastVisit.ToDateTime(TimeOnly.MinValue)
            });
        }

        return result.OrderBy(p => p.Name).ToList();
    }
}
