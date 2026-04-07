using PiedraAzul.Domain.Entities.Operations;

namespace PiedraAzul.Domain.Repositories;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task DeleteAsync(Appointment appointment, CancellationToken cancellationToken = default);

    Task<bool> ExistsBySlotAndDateAsync(
        Guid doctorAvailabilitySlotId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Appointment>> ListByDoctorAsync(
        string doctorId,
        DateOnly? date = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Appointment>> ListByPatientUserAsync(
        string patientUserId,
        DateOnly? date = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Appointment>> ListByPatientGuestAsync(
        string patientGuestId,
        DateOnly? date = null,
        CancellationToken cancellationToken = default);
}