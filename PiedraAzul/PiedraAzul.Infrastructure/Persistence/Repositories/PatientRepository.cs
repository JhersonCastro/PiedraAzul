using Microsoft.EntityFrameworkCore;
using PiedraAzul.Domain.Entities.Profiles.Patients;
using PiedraAzul.Domain.Repositories;
using PiedraAzul.Infrastructure.Persistence;

namespace PiedraAzul.Infrastructure.Persistence.Repositories;

public class PatientRepository(AppDbContext context) : IPatientRepository
{
    public async Task<RegisteredPatient?> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return await context.Patients
            .OfType<RegisteredPatient>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);
    }

    public async Task<IReadOnlyList<RegisteredPatient>> SearchAsync(string text, CancellationToken ct = default)
    {
        return await context.Patients
            .OfType<RegisteredPatient>()
            .Where(x => EF.Functions.Like(x.Name, $"%{text}%"))
            .OrderBy(x => x.Name)
            .Take(10)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task AddAsync(RegisteredPatient patient, CancellationToken ct = default)
    {
        await context.Patients.AddAsync(patient, ct);
    }

    public Task UpdateAsync(RegisteredPatient patient, CancellationToken ct = default)
    {
        context.Patients.Update(patient);
        return Task.CompletedTask;
    }
}