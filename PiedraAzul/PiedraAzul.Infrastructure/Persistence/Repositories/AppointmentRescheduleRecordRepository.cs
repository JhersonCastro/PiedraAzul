using Microsoft.EntityFrameworkCore;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Infrastructure.Persistence.Repositories;

public class AppointmentRescheduleRecordRepository : IAppointmentRescheduleRecordRepository
{
    private readonly AppDbContext _context;

    public AppointmentRescheduleRecordRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AppointmentRescheduleRecord record, CancellationToken ct = default)
    {
        await _context.AppointmentRescheduleRecords.AddAsync(record, ct);
    }

    public async Task<AppointmentRescheduleRecord?> GetByNewAppointmentIdAsync(
        Guid newAppointmentId,
        CancellationToken ct = default)
    {
        return await _context.AppointmentRescheduleRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.NewAppointmentId == newAppointmentId, ct);
    }

    public async Task<IReadOnlyList<AppointmentRescheduleRecord>> GetByNewAppointmentIdsAsync(
        IReadOnlyCollection<Guid> newAppointmentIds,
        CancellationToken ct = default)
    {
        if (newAppointmentIds.Count == 0)
            return [];

        return await _context.AppointmentRescheduleRecords
            .AsNoTracking()
            .Where(x => newAppointmentIds.Contains(x.NewAppointmentId))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AppointmentRescheduleRecord>> GetByRootAppointmentIdsAsync(
        IReadOnlyCollection<Guid> rootAppointmentIds,
        CancellationToken ct = default)
    {
        if (rootAppointmentIds.Count == 0)
            return [];

        return await _context.AppointmentRescheduleRecords
            .AsNoTracking()
            .Where(x => rootAppointmentIds.Contains(x.RootAppointmentId))
            .ToListAsync(ct);
    }
}
