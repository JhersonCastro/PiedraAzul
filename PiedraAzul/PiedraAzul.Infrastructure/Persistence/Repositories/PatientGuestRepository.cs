using Microsoft.EntityFrameworkCore;
using PiedraAzul.Domain.Entities.Profiles.Patients;
using PiedraAzul.Domain.Repositories;
using PiedraAzul.Infrastructure.Persistence;

namespace PiedraAzul.Infrastructure.Persistence.Repositories;

public class PatientGuestRepository(AppDbContext context) : IPatientGuestRepository
{
    public async Task<GuestPatient?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await context.Patients
            .OfType<GuestPatient>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<IReadOnlyList<GuestPatient>> SearchAsync(string text, CancellationToken ct = default)
    {
        return await context.Patients
            .OfType<GuestPatient>()
            .Where(x =>
                EF.Functions.Like(x.Name, $"%{text}%") ||
                EF.Functions.Like(x.Phone, $"%{text}%"))
            .OrderBy(x => x.Name)
            .Take(10)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task AddAsync(GuestPatient patient, CancellationToken ct = default)
    {
        await context.Patients.AddAsync(patient, ct);
    }

    public Task UpdateAsync(GuestPatient patient, CancellationToken ct = default)
    {
        context.Patients.Update(patient);
        return Task.CompletedTask;
    }

    public async Task<List<GuestPatient>> GetByIdsAsync(List<string> ids, CancellationToken ct)
    {
        if (ids == null || ids.Count == 0)
            return [];

        var idSet = ids.ToHashSet();

        return await context.Patients
            .OfType<GuestPatient>()
            .Where(x => idSet.Contains(x.Id))
            .AsNoTracking()
            .ToListAsync(ct);
    }
}