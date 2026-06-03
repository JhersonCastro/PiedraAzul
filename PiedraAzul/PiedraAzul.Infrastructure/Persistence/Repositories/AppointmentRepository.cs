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

    public async Task<Appointment?> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Appointments
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
        // Un slot está "tomado" si tiene una cita Active, Completed o NoShow.
        // Cancelled y Rescheduled liberan el cupo.
        return await _context.Appointments
            .AnyAsync(x =>
                x.DoctorAvailabilitySlotId == doctorAvailabilitySlotId &&
                x.Date == date &&
                (x.Status == Domain.Entities.Operations.AppointmentStatus.Active    ||
                 x.Status == Domain.Entities.Operations.AppointmentStatus.Completed ||
                 x.Status == Domain.Entities.Operations.AppointmentStatus.NoShow),
                ct);
    }

    public async Task<IReadOnlyList<Appointment>> ListByDoctorAsync(
        string doctorId,
        DateOnly? date = null,
        CancellationToken ct = default)
    {
        // Muestra todas las citas excepto Rescheduled (que es un soft-delete de auditoría)
        var query = _context.Appointments
            .Where(x => x.DoctorId == doctorId &&
                        x.Status != Domain.Entities.Operations.AppointmentStatus.Rescheduled);

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
        // Sin filtro de status: el paciente debe ver Completed, NoShow y Cancelled
        // además de Active. El filtrado visual lo hace el cliente con badges.
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
        // Sin filtro de status: el invitado también ve su historial completo.
        var query = _context.Appointments
            .Where(x => x.PatientGuestId == patientGuestId);

        if (date.HasValue)
            query = query.Where(x => x.Date == date.Value);

        return await query
            .AsNoTracking()
            .ToListAsync(ct);
    }
}