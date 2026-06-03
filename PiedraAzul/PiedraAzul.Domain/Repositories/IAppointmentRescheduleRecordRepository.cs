using PiedraAzul.Domain.Entities.Operations;

namespace PiedraAzul.Domain.Repositories;

public interface IAppointmentRescheduleRecordRepository
{
    Task AddAsync(AppointmentRescheduleRecord record, CancellationToken cancellationToken = default);

    /// <summary>Registro cuya cita nueva es <paramref name="newAppointmentId"/>. Sirve para calcular el linaje (Root).</summary>
    Task<AppointmentRescheduleRecord?> GetByNewAppointmentIdAsync(
        Guid newAppointmentId,
        CancellationToken cancellationToken = default);

    /// <summary>Registros cuya cita nueva está en la lista dada (para marcar qué citas provienen de un reagendamiento).</summary>
    Task<IReadOnlyList<AppointmentRescheduleRecord>> GetByNewAppointmentIdsAsync(
        IReadOnlyCollection<Guid> newAppointmentIds,
        CancellationToken cancellationToken = default);

    /// <summary>Todo el historial de los linajes indicados, en una sola query.</summary>
    Task<IReadOnlyList<AppointmentRescheduleRecord>> GetByRootAppointmentIdsAsync(
        IReadOnlyCollection<Guid> rootAppointmentIds,
        CancellationToken cancellationToken = default);
}
