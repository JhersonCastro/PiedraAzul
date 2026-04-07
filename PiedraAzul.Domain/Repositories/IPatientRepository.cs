using PiedraAzul.Domain.Entities.Profiles.Patients;

namespace PiedraAzul.Domain.Repositories;

public interface IPatientRepository
{
    Task<RegisteredPatient?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(RegisteredPatient patient, CancellationToken cancellationToken = default);
    Task UpdateAsync(RegisteredPatient patient, CancellationToken cancellationToken = default);
}