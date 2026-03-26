using Microsoft.EntityFrameworkCore;
using PiedraAzul.Data;
using PiedraAzul.Data.Models;

namespace PiedraAzul.ApplicationServices.Services
{
    public interface IAppointmentService
    {
        Task<Appointment> CreateAppointmentAsync(
            Appointment appointment,
            string? patientUserId = null,
            string? patientGuestId = null);

        Task<List<(DoctorAvailabilitySlot Slot, bool IsAvailable)>> GetDoctorDaySlotsAsync(
            string doctorUserId,
            DateTime date);

        Task<List<Appointment>> GetDoctorAppointmentsAsync(
            string doctorUserId,
            DateTime date = default);

        Task<DoctorAppointmentsSearchResult> SearchDoctorAppointmentsAsync(
            string doctorUserId,
            DateTime date,
            int pageNumber = 1,
            int pageSize = 50);

        Task<List<Appointment>> GetPatientAppointmentsAsync(
            string? patientUserId,
            string? patientGuestId,
            DateTime date = default);
    }

    public class DoctorAppointmentSearchItem
    {
        public Guid AppointmentId { get; set; }
        public string TimeRange { get; set; } = string.Empty;
        public string Patient { get; set; } = string.Empty;
        public string PatientType { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class DoctorAppointmentsSearchResult
    {
        public List<DoctorAppointmentSearchItem> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }

    public class AppointmentService(IDbContextFactory<AppDbContext> dbContextFactory) : IAppointmentService
    {
        public async Task<Appointment> CreateAppointmentAsync(
            Appointment appointment,
            string? patientUserId = null,
            string? patientGuestId = null)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            if (string.IsNullOrWhiteSpace(appointment.DoctorUserId))
                throw new ArgumentException("DoctorUserId is required.");

            var doctor = await context.Users
                .Include(u => u.DoctorProfile)
                .FirstOrDefaultAsync(u => u.Id == appointment.DoctorUserId);

            if (doctor == null || doctor.DoctorProfile == null)
                throw new InvalidOperationException("Invalid doctor.");

            var slot = await context.DoctorAvailabilitySlots
                .FirstOrDefaultAsync(s =>
                    s.Id == appointment.DoctorAvailabilitySlotId &&
                    s.DoctorUserId == appointment.DoctorUserId);

            if (slot == null)
                throw new InvalidOperationException("Invalid slot.");

            if (slot.DayOfWeek != appointment.Date.DayOfWeek)
                throw new InvalidOperationException("Date does not match slot.");

            if (!string.IsNullOrWhiteSpace(patientUserId))
            {
                appointment.PatientUserId = patientUserId;
                appointment.PatientGuestId = null;
            }
            else if (!string.IsNullOrWhiteSpace(patientGuestId))
            {
                appointment.PatientGuestId = patientGuestId;
                appointment.PatientUserId = null;
            }
            else
            {
                throw new ArgumentException("Patient required.");
            }

            var exists = await context.Appointments.AnyAsync(a =>
                a.DoctorAvailabilitySlotId == appointment.DoctorAvailabilitySlotId &&
                a.Date == appointment.Date);

            if (exists)
                throw new InvalidOperationException("Slot already taken.");

            context.Add(appointment);
            await context.SaveChangesAsync();

            return appointment;
        }

        public async Task<List<Appointment>> GetDoctorAppointmentsAsync(
            string doctorUserId,
            DateTime date = default)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            var query = context.Appointments
                .Where(a => a.DoctorUserId == doctorUserId);

            if (date != default)
            {
                var day = date.Date;
                query = query.Where(a => a.Date == day);
            }

            return await query
                .Include(a => a.Patient)
                .Include(a => a.PatientGuest)
                .Include(a => a.DoctorAvailabilitySlot)
                .ToListAsync();
        }

        public async Task<DoctorAppointmentsSearchResult> SearchDoctorAppointmentsAsync(
            string doctorUserId,
            DateTime date,
            int pageNumber = 1,
            int pageSize = 50)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            var colombiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");

            var localDate = TimeZoneInfo.ConvertTime(date, colombiaTimeZone).Date;
            var utcStart = TimeZoneInfo.ConvertTimeToUtc(localDate, colombiaTimeZone);
            var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localDate.AddDays(1), colombiaTimeZone);

            var query = context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Include(a => a.PatientGuest)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.DoctorProfile)
                .Include(a => a.DoctorAvailabilitySlot)
                .Where(a => a.DoctorUserId == doctorUserId &&
                            a.Date >= utcStart &&
                            a.Date < utcEnd);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(a => a.DoctorAvailabilitySlot.StartTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new DoctorAppointmentSearchItem
                {
                    AppointmentId = a.Id,
                    TimeRange = $"{a.DoctorAvailabilitySlot.StartTime:hh\\:mm} - {a.DoctorAvailabilitySlot.EndTime:hh\\:mm}",
                    Patient = a.Patient != null
                        ? a.Patient.Name
                        : a.PatientGuest != null
                            ? a.PatientGuest.PatientName
                            : "Sin paciente",
                    PatientType = a.PatientUserId != null ? "Registrado" : "Invitado",
                    Specialty = a.Doctor.DoctorProfile.Specialty.ToString(),
                    Status = "Programada",
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return new DoctorAppointmentsSearchResult
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<List<(DoctorAvailabilitySlot Slot, bool IsAvailable)>> GetDoctorDaySlotsAsync(
            string doctorUserId,
            DateTime date)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            var dayOfWeek = date.DayOfWeek;
            var day = date.Date;

            var slots = await context.DoctorAvailabilitySlots
                .Where(s => s.DoctorUserId == doctorUserId && s.DayOfWeek == dayOfWeek)
                .ToListAsync();

            var occupied = await context.Appointments
                .Where(a => a.DoctorUserId == doctorUserId && a.Date == day)
                .Select(a => a.DoctorAvailabilitySlotId)
                .ToHashSetAsync();

            return slots
                .Select(slot => (
                    Slot: slot,
                    IsAvailable: !occupied.Contains(slot.Id)
                ))
                .ToList();
        }

        public async Task<List<Appointment>> GetPatientAppointmentsAsync(
            string? patientUserId,
            string? patientGuestId,
            DateTime date = default)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            var query = context.Appointments.AsQueryable();

            if (!string.IsNullOrWhiteSpace(patientUserId))
            {
                query = query.Where(a => a.PatientUserId == patientUserId);
            }
            else if (!string.IsNullOrWhiteSpace(patientGuestId))
            {
                query = query.Where(a => a.PatientGuestId == patientGuestId);
            }
            else
            {
                throw new ArgumentException("Debe proporcionar paciente.");
            }

            if (date != default)
            {
                var day = date.Date;
                query = query.Where(a => a.Date == day);
            }

            return await query
                .Include(a => a.Patient)
                .Include(a => a.PatientGuest)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.DoctorProfile)
                .Include(a => a.DoctorAvailabilitySlot)
                .ToListAsync();
        }
    }
}