using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Entities.Shared.Enums;

namespace PiedraAzul.Domain.Repositories;

public interface IDoctorRepository
{
    Task<Doctor?> GetByIdAsync(string doctorId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string doctorId, CancellationToken cancellationToken = default);

    Task AddAsync(Doctor doctor, CancellationToken cancellationToken = default);

    Task UpdateAsync(Doctor doctor, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Doctor>> GetBySpecialtyAsync(
    DoctorType specialty,
    CancellationToken cancellationToken = default);
}