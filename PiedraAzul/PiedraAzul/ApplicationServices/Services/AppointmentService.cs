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

        Task<List<Appointment>> GetPatientAppointmentsAsync(
            string? patientUserId,
            string? patientGuestId,
            DateTime date = default);
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
                throw new ArgumentException("DoctorUserId is required.", nameof(appointment));

            var doctor = await context.Users
                .Include(u => u.DoctorProfile)
                .FirstOrDefaultAsync(u => u.Id == appointment.DoctorUserId);

            if (doctor == null)
                throw new ArgumentNullException(nameof(appointment.DoctorUserId));

            if (doctor.DoctorProfile == null)
                throw new InvalidOperationException("The selected user is not a doctor.");

            var slot = await context.DoctorAvailabilitySlots
                .FirstOrDefaultAsync(s =>
                    s.Id == appointment.DoctorAvailabilitySlotId &&
                    s.DoctorUserId == appointment.DoctorUserId);

            if (slot == null)
                throw new InvalidOperationException("The selected slot does not belong to the doctor.");

            if (slot.DayOfWeek != appointment.Date.DayOfWeek)
                throw new InvalidOperationException("The appointment date does not match the slot day of week.");

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
                throw new ArgumentException("Either patientUserId or patientGuestId must be provided.");
            }

            var exists = await context.Appointments.AnyAsync(a =>
                a.DoctorAvailabilitySlotId == appointment.DoctorAvailabilitySlotId &&
                a.Date == appointment.Date);

            if (exists)
                throw new InvalidOperationException("This slot is already occupied for that date.");

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

        public async Task<List<(DoctorAvailabilitySlot Slot, bool IsAvailable)>> GetDoctorDaySlotsAsync(
            string doctorUserId,
            DateTime date)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            var doctorExists = await context.Users
                .AnyAsync(u => u.Id == doctorUserId && u.DoctorProfile != null);

            if (!doctorExists)
                throw new ArgumentNullException(nameof(doctorUserId));

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
                query = query.Where(a => a.PatientGuestId == patientGuestId);
            }
            else
            {
                throw new ArgumentException("Debe proporcionar patientUserId o patientGuestId.");
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