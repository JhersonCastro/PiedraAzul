using PiedraAzul.Domain.Entities.Profiles.Doctor;

namespace PiedraAzul.Domain.Repositories;

public interface IDoctorAvailabilitySlotRepository
{
    Task<DoctorAvailabilitySlot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoctorAvailabilitySlot>> ListByDoctorAsync(string doctorId, CancellationToken cancellationToken = default);
    Task AddAsync(DoctorAvailabilitySlot slot, CancellationToken cancellationToken = default);
    Task UpdateAsync(DoctorAvailabilitySlot slot, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}