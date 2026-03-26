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
        public string PatientName { get; set; } = string.Empty; // nuevo
        public string PatientType { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Start { get; set; } // nuevo
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
        private const int DefaultPageSize = 50;
        private const int MaxPageSize = 200;

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
                var patient = await context.Users
                    .Include(u => u.PatientProfile)
                    .FirstOrDefaultAsync(u => u.Id == patientUserId);

                if (patient == null)
                    throw new ArgumentNullException(nameof(patientUserId));

                if (patient.PatientProfile == null)
                    throw new InvalidOperationException("The selected user is not a patient.");

                appointment.PatientUserId = patient.Id;
                appointment.PatientGuestId = null;
            }
            else if (!string.IsNullOrWhiteSpace(patientGuestId))
            {
                var patientGuest = await context.PatientGuests
                    .FirstOrDefaultAsync(p => p.PatientIdentification == patientGuestId);

                if (patientGuest == null)
                    throw new ArgumentNullException(nameof(patientGuestId));

                appointment.PatientGuestId = patientGuest.PatientIdentification;
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

            var doctorExists = await context.Users
                .AnyAsync(u => u.Id == doctorUserId && u.DoctorProfile != null);

            if (!doctorExists)
                throw new ArgumentNullException(nameof(doctorUserId));

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

            if (string.IsNullOrWhiteSpace(doctorUserId))
                throw new ArgumentException("DoctorUserId is required.", nameof(doctorUserId));

            var doctorExists = await context.Users
                .AnyAsync(u => u.Id == doctorUserId && u.DoctorProfile != null);

            if (!doctorExists)
                throw new ArgumentNullException(nameof(doctorUserId));

            var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
            var safePageSize = pageSize < 1
                ? DefaultPageSize
                : Math.Min(pageSize, MaxPageSize);

            var colombiaTimeZone = ResolveColombiaTimeZone();
            var utcDate = date.Kind == DateTimeKind.Utc ? date : date.ToUniversalTime();
            var localDate = TimeZoneInfo.ConvertTimeFromUtc(utcDate, colombiaTimeZone).Date;
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
                .Skip((safePageNumber - 1) * safePageSize)
                .Take(safePageSize)
                .Select(a => new DoctorAppointmentSearchItem
                {
                    AppointmentId = a.Id,
                    TimeRange = $"{a.DoctorAvailabilitySlot.StartTime:hh\\:mm} - {a.DoctorAvailabilitySlot.EndTime:hh\\:mm}",
                    Patient = a.Patient != null
                        ? a.Patient.Name
                        : a.PatientGuest != null
                            ? a.PatientGuest.PatientName
                            : "Sin paciente",
                    PatientName = a.Patient != null
                        ? a.Patient.Name
                        : a.PatientGuest != null
                            ? a.PatientGuest.PatientName
                            : "Sin paciente",
                    PatientType = a.PatientUserId != null ? "Registrado" : "Invitado",
                    Specialty = a.Doctor.DoctorProfile.Specialty.ToString(),
                    Status = "Programada",
                    Start = a.Date.Add(a.DoctorAvailabilitySlot.StartTime),
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return new DoctorAppointmentsSearchResult
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = safePageNumber,
                PageSize = safePageSize
            };
        }

        private static TimeZoneInfo ResolveColombiaTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            }
            catch (InvalidTimeZoneException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            }
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
                var patientExists = await context.Users
                    .AnyAsync(u => u.Id == patientUserId && u.PatientProfile != null);

                if (!patientExists)
                    throw new ArgumentNullException(nameof(patientUserId));

                query = query.Where(a => a.PatientUserId == patientUserId);
            }
            else if (!string.IsNullOrWhiteSpace(patientGuestId))
            {
                var guestExists = await context.PatientGuests
                    .AnyAsync(g => g.PatientIdentification == patientGuestId);

                if (!guestExists)
                    throw new ArgumentNullException(nameof(patientGuestId));

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