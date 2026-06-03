using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.Appointments;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Application.Common.Helpers;

/// <summary>
/// Adjunta el historial de reagendamientos a una lista de citas actuales.
/// Usa como máximo 2 queries a la tabla de records (sin importar el tamaño de la lista)
/// más la resolución batch de nombres/roles vía Identity.
/// </summary>
public static class AppointmentRescheduleEnricher
{
    public static async Task EnrichAsync(
        IReadOnlyList<AppointmentDto> appointments,
        IAppointmentRescheduleRecordRepository recordRepository,
        IIdentityService identityService,
        CancellationToken ct)
    {
        if (appointments.Count == 0)
            return;

        var apptIds = appointments.Select(a => a.Id).ToList();

        // 1. ¿Cuáles citas actuales provienen de un reagendamiento? (badge) y ¿cuál es su linaje?
        var fromRecords = await recordRepository.GetByNewAppointmentIdsAsync(apptIds, ct);
        if (fromRecords is null || fromRecords.Count == 0)
            return;

        var rootByNewAppointmentId = fromRecords
            .GroupBy(r => r.NewAppointmentId)
            .ToDictionary(g => g.Key, g => g.First().RootAppointmentId);

        // 2. Todo el historial de esos linajes en una sola query.
        var roots = rootByNewAppointmentId.Values.Distinct().ToList();
        var allRecords = await recordRepository.GetByRootAppointmentIdsAsync(roots, ct);
        if (allRecords is null || allRecords.Count == 0)
            return;

        // 3. Resolver nombres y roles (batch).
        var userIds = allRecords
            .SelectMany(r => new[] { r.RescheduledByUserId, r.OriginalDoctorId, r.NewDoctorId })
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        var users = await identityService.GetByIds(userIds);
        var nameById = users.ToDictionary(u => u.Id, u => u.Name);

        var rolesByUserId = new Dictionary<string, string[]>();
        foreach (var rescheduledBy in allRecords.Select(r => r.RescheduledByUserId).Distinct())
        {
            var roles = await identityService.GetRolesByUser(rescheduledBy);
            rolesByUserId[rescheduledBy] = roles.ToArray();
        }

        // 4. Construir las entradas agrupadas por linaje, en orden cronológico.
        var entriesByRoot = allRecords
            .GroupBy(r => r.RootAppointmentId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(r => r.RescheduledAt)
                    .Select(r => new AppointmentRescheduleEntryDto
                    {
                        RescheduledByUserId = r.RescheduledByUserId,
                        RescheduledByName = nameById.GetValueOrDefault(r.RescheduledByUserId, ""),
                        RescheduledByRoles = rolesByUserId.GetValueOrDefault(r.RescheduledByUserId, []),
                        RescheduledAt = r.RescheduledAt,
                        OriginalDate = r.OriginalDate.ToDateTime(TimeOnly.MinValue),
                        NewDate = r.NewDate.ToDateTime(TimeOnly.MinValue),
                        OriginalDoctorId = r.OriginalDoctorId,
                        OriginalDoctorName = nameById.GetValueOrDefault(r.OriginalDoctorId, ""),
                        NewDoctorId = r.NewDoctorId,
                        NewDoctorName = nameById.GetValueOrDefault(r.NewDoctorId, "")
                    })
                    .ToList());

        // 5. Adjuntar a cada cita actual.
        foreach (var appt in appointments)
        {
            if (!rootByNewAppointmentId.TryGetValue(appt.Id, out var root))
                continue;

            appt.WasRescheduled = true;
            if (entriesByRoot.TryGetValue(root, out var entries))
                appt.RescheduleHistory = entries;
        }
    }
}
