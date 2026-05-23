using Microsoft.EntityFrameworkCore;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Repositories;
using PiedraAzul.Infrastructure.Persistence;

namespace PiedraAzul.Infrastructure.Persistence.Repositories;

public class DoctorAvailabilitySlotRepository : IDoctorAvailabilitySlotRepository
{
    private readonly AppDbContext _context;

    public DoctorAvailabilitySlotRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DoctorAvailabilitySlot?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.DoctorAvailabilitySlots
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<IReadOnlyList<DoctorAvailabilitySlot>> ListByDoctorAsync(string doctorId, bool includeDeleted = false, CancellationToken ct = default)
    {
        return await _context.DoctorAvailabilitySlots
            .Where(x => x.DoctorId == doctorId && (includeDeleted || !x.IsDeleted))
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<DoctorAvailabilitySlot?> GetExactAsync(string doctorId, DayOfWeek day, TimeSpan start, TimeSpan end, CancellationToken ct = default)
    {
        return await _context.DoctorAvailabilitySlots
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.DoctorId == doctorId && x.DayOfWeek == day && x.StartTime == start && x.EndTime == end, ct);
    }

    public async Task AddAsync(DoctorAvailabilitySlot slot, CancellationToken ct = default)
    {
        await _context.DoctorAvailabilitySlots.AddAsync(slot, ct);
    }

    public Task UpdateAsync(DoctorAvailabilitySlot slot, CancellationToken ct = default)
    {
        _context.DoctorAvailabilitySlots.Update(slot);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.DoctorAvailabilitySlots
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return;

        entity.SoftDelete();
    }

    public async Task<List<DoctorAvailabilitySlot>> GetByIdsAsync(List<Guid> ids, CancellationToken ct)
    {
        if (ids == null || ids.Count == 0)
            return [];

        // No filtramos IsDeleted: las citas existentes pueden referenciar slots ya eliminados
        return await _context.DoctorAvailabilitySlots
            .Where(x => ids.Contains(x.Id))
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DoctorAvailabilitySlot>> GetDoctorDaySlotsAsync(
        string doctorId,
        DateOnly date,
        CancellationToken ct)
    {
        return await _context.DoctorAvailabilitySlots
            .Where(x => x.DoctorId == doctorId && x.DayOfWeek == date.DayOfWeek && !x.IsDeleted)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
