using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using PiedraAzul.Data;
using PiedraAzul.Data.Models;

namespace PiedraAzul.ApplicationServices.Services
{
    public interface IAppointmentService
    {
        Task<Appointment> CreateAppointmentAsync(Appointment appointment, string? patientUserId = null, string? patientGuestId = null);

        Task<List<(DoctorAvailabilitySlot Slot, bool IsAvailable)>> GetDoctorDaySlotsAsync(Guid doctorId, DateTime date);

        Task<List<Appointment>> GetDoctorAppointmentsAsync(string doctorId, DateTime date = default);
        Task<DoctorAppointmentsSearchResult> SearchDoctorAppointmentsAsync(
            string doctorId,
            DateTime date,
            int pageNumber = 1,
            int pageSize = 50);
        Task<List<Appointment>> GetPatientAppointmentsAsync(string? patientId, string? patientGuestId, DateTime date = default);

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
        public List<DoctorAppointmentSearchItem> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }

    public class AppointmentService(IDbContextFactory<AppDbContext> dbContextFactory) : IAppointmentService
    {
        public async Task<Appointment> CreateAppointmentAsync(Appointment appointment, string? patientUserId, string? patientGuestId = null)
        {
            using var context = await dbContextFactory.CreateDbContextAsync();
            if(!string.IsNullOrWhiteSpace(patientUserId))
            {
                var patient = await context.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == patientUserId);
                if (patient == null) throw new ArgumentNullException(nameof(patientUserId));
             
                appointment.PatientId = patient.PatientId;
            }else if(!string.IsNullOrWhiteSpace(patientGuestId))
            {
                var patientGuest = await context.PatientGuests.FirstOrDefaultAsync(p => p.PatientIdentification == patientGuestId);
                if (patientGuest == null) throw new ArgumentNullException(nameof(patientGuestId));
                appointment.PatientGuestId = patientGuest.PatientIdentification;
            }
            else
            {
                throw new ArgumentException("Either patientUserId or patientGuestId must be provided");
            }
            context.Add(appointment);
            await context.SaveChangesAsync();

            return appointment;
        }

        public async Task<List<Appointment>> GetDoctorAppointmentsAsync(string doctorId, DateTime date = default)
        {
            using var context = await dbContextFactory.CreateDbContextAsync();

            var user = await context.DoctorProfiles
                .FirstOrDefaultAsync(doc => doc.UserId == doctorId);

            if (user == null)
                throw new ArgumentNullException(nameof(doctorId));

            var query = context.Appointments.AsQueryable();

            query = query.Where(a => a.DoctorId == user.DoctorId);

            if (date != default)
            {
                var day = date.Date;
                query = query.Where(a => a.Date == day);
            }

            return await query.ToListAsync();
        }

        public async Task<DoctorAppointmentsSearchResult> SearchDoctorAppointmentsAsync(
            string doctorId,
            DateTime date,
            int pageNumber = 1,
            int pageSize = 50)
        {
            if (date == default)
                throw new ArgumentException("Date is required", nameof(date));

            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 50;

            using var context = await dbContextFactory.CreateDbContextAsync();

            var doctor = await context.DoctorProfiles
                .Include(d => d.User)
                .FirstOrDefaultAsync(doc => doc.UserId == doctorId);

            if (doctor == null)
                throw new ArgumentNullException(nameof(doctorId));

            var colombiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
            var localDate = TimeZoneInfo.ConvertTime(date, colombiaTimeZone).Date;
            var localStart = localDate;
            var localEnd = localDate.AddDays(1);
            var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, colombiaTimeZone);
            var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localEnd, colombiaTimeZone);

            var query = context.Appointments
                .AsNoTracking()
                .Include(a => a.DoctorAvailabilitySlot)
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.PatientGuest)
                .Where(a => a.DoctorId == doctor.DoctorId &&
                            a.Date >= utcStart &&
                            a.Date < utcEnd);

            var totalCount = await query.CountAsync();
            var nowLocal = TimeZoneInfo.ConvertTime(DateTime.UtcNow, colombiaTimeZone);

            var appointments = await query
                .OrderBy(a => a.DoctorAvailabilitySlot.StartTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new DoctorAppointmentSearchItem
                {
                    AppointmentId = a.Id,
                    TimeRange = $"{a.DoctorAvailabilitySlot.StartTime:hh\\:mm} - {a.DoctorAvailabilitySlot.EndTime:hh\\:mm}",
                    Patient = a.Patient != null
                        ? a.Patient.User.Name
                        : a.PatientGuest != null
                            ? $"{a.PatientGuest.PatientName} ({a.PatientGuest.PatientIdentification})"
                            : "Sin paciente",
                    PatientType = a.PatientId != null ? "Registrado" : "Invitado",
                    Specialty = doctor.Specialty.ToString(),
                    Status = a.Date.Date < nowLocal.Date ? "Finalizada" : "Programada",
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return new DoctorAppointmentsSearchResult
            {
                Items = appointments,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<List<(DoctorAvailabilitySlot Slot, bool IsAvailable)>> GetDoctorDaySlotsAsync(Guid doctorId, DateTime date)
        {
            using var context = await dbContextFactory.CreateDbContextAsync();

            var dayOfWeek = date.DayOfWeek;
            var day = date.Date;

            var slots = await context.DoctorAvailabilitySlots
                .Where(s => s.DoctorId == doctorId && s.DayOfWeek == dayOfWeek)
                .ToListAsync();

            var occupied = await context.Appointments
                .Where(a => a.DoctorId == doctorId && a.Date == day)
                .Select(a => a.DoctorAvailabilitySlotId)
                .ToHashSetAsync();

            var result = slots
                .Select(slot => (
                    Slot: slot,
                    IsAvailable: !occupied.Contains(slot.Id)
                ))
                .ToList();

            return result;
        }

        public async Task<List<Appointment>> GetPatientAppointmentsAsync(
            string? patientId,
            string? patientGuestId,
            DateTime date = default)
        {
            using var context = await dbContextFactory.CreateDbContextAsync();

            var query = context.Appointments.AsQueryable();

            if (!string.IsNullOrEmpty(patientId))
            {
                var user = await context.PatientProfiles
                    .FirstOrDefaultAsync(p => p.UserId == patientId);

                if (user == null)
                    throw new ArgumentNullException(nameof(patientId));

                query = query.Where(a => a.PatientId == user.PatientId);
            }
            else if (!string.IsNullOrEmpty(patientGuestId))
            {
                query = query.Where(a => a.PatientGuestId == patientGuestId);
            }
            else
            {
                throw new ArgumentException("Debe proporcionar patientId o patientGuestId");
            }


            if (date != default)
            {
                var day = date.Date;
                query = query.Where(a => a.Date == day);
            }

            return await query
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.PatientGuest)
                .Include(a => a.DoctorAvailabilitySlot)
                .ToListAsync();
        }
    }
}
