using Microsoft.EntityFrameworkCore;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Repositories;
using PiedraAzul.Infrastructure.Persistence;

namespace PiedraAzul.Infrastructure.Persistence.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly AppDbContext _context;

    public AppointmentRepository(AppDbContext context)
    {
        _context = context;
    }
    public async Task<Appointment?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task AddAsync(Appointment appointment, CancellationToken ct = default)
    {
        await _context.Appointments.AddAsync(appointment, ct);
    }

    public Task UpdateAsync(Appointment appointment, CancellationToken ct = default)
    {
        _context.Appointments.Update(appointment);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Appointment appointment, CancellationToken ct = default)
    {
        _context.Appointments.Remove(appointment);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsBySlotAndDateAsync(
        Guid doctorAvailabilitySlotId,
        DateOnly date,
        CancellationToken ct = default)
    {
        return await _context.Appointments
            .AnyAsync(x =>
                x.DoctorAvailabilitySlotId == doctorAvailabilitySlotId &&
                x.Date == date,
                ct);
    }

    public async Task<IReadOnlyList<Appointment>> ListByDoctorAsync(
        string doctorId,
        DateOnly? date = null,
        CancellationToken ct = default)
    {
        var query = _context.Appointments
            .Where(x => x.DoctorId == doctorId);

        if (date.HasValue)
            query = query.Where(x => x.Date == date.Value);

        return await query
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Appointment>> ListByPatientUserAsync(
        string patientUserId,
        DateOnly? date = null,
        CancellationToken ct = default)
    {
        var query = _context.Appointments
            .Where(x => x.PatientUserId == patientUserId);

        if (date.HasValue)
            query = query.Where(x => x.Date == date.Value);

        return await query
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Appointment>> ListByPatientGuestAsync(
        string patientGuestId,
        DateOnly? date = null,
        CancellationToken ct = default)
    {
        var query = _context.Appointments
            .Where(x => x.PatientGuestId == patientGuestId);

        if (date.HasValue)
            query = query.Where(x => x.Date == date.Value);

        return await query
            .AsNoTracking()
            .ToListAsync(ct);
    }
}