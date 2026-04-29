using Microsoft.EntityFrameworkCore;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Entities.Shared.Enums;
using PiedraAzul.Domain.Repositories;
using PiedraAzul.Infrastructure.Persistence;

namespace PiedraAzul.Infrastructure.Persistence.Repositories;

public class DoctorRepository(AppDbContext context) : IDoctorRepository
{
    public async Task<Doctor?> GetByIdAsync(string doctorId, CancellationToken ct = default)
    {
        return await context.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == doctorId, ct);
    }

    public async Task<bool> ExistsAsync(string doctorId, CancellationToken ct = default)
    {
        return await context.Doctors
            .AnyAsync(x => x.Id == doctorId, ct);
    }

    public async Task AddAsync(Doctor doctor, CancellationToken ct = default)
    {
        await context.Doctors.AddAsync(doctor, ct);
    }

    public Task UpdateAsync(Doctor doctor, CancellationToken ct = default)
    {
        context.Doctors.Update(doctor);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Doctor>> GetBySpecialtyAsync(
        DoctorType specialty,
        CancellationToken ct = default)
    {
        return await context.Doctors
            .Where(x => x.Specialty == specialty)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}