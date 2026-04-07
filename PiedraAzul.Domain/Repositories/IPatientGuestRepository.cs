using PiedraAzul.Domain.Entities.Profiles.Patients;

namespace PiedraAzul.Domain.Repositories;

public interface IPatientGuestRepository
{
    Task<GuestPatient?> GetByIdAsync(string patientIdentification, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GuestPatient>> SearchAsync(string text, CancellationToken cancellationToken = default);
    Task AddAsync(GuestPatient patient, CancellationToken cancellationToken = default);
    Task UpdateAsync(GuestPatient patient, CancellationToken cancellationToken = default);
}