using PiedraAzul.Domain.Common.Exceptions;
using PiedraAzul.Domain.Entities.Profiles.Doctor;

namespace PiedraAzul.Domain.Entities.Operations
{
    public enum AppointmentStatus { Active, Cancelled, Rescheduled, Completed, NoShow }

    public class Appointment
    {
        public Guid Id { get; private set; }

        public string DoctorId { get; private set; }

        public Guid DoctorAvailabilitySlotId { get; private set; }
        public DoctorAvailabilitySlot Slot { get; private set; }

        public string? PatientUserId { get; private set; }
        public string? PatientGuestId { get; private set; }

        public DateOnly Date { get; private set; }

        public DateTime CreatedAt { get; private set; }

        /// <summary>Active (default), Cancelled o Rescheduled (soft-delete para auditoría).</summary>
        public AppointmentStatus Status { get; private set; } = AppointmentStatus.Active;

        /// <summary>Si fue reagendada, apunta al Id de la nueva cita.</summary>
        public Guid? RescheduledToId { get; private set; }

        private Appointment() { }

        private Appointment(
            string doctorId,
            Guid slotId,
            DateOnly date,
            string? patientUserId,
            string? patientGuestId)
        {
            Id = Guid.NewGuid();
            DoctorId = doctorId;
            DoctorAvailabilitySlotId = slotId;
            Date = date;
            PatientUserId = patientUserId;
            PatientGuestId = patientGuestId;
            CreatedAt = DateTime.UtcNow;
        }

        public static Appointment Create(
    DoctorAvailabilitySlot slot,
    DateOnly date,
    string doctorId,
    string? patientUserId,
    string? patientGuestId)
        {
            if ((patientUserId is null && patientGuestId is null) ||
                (patientUserId is not null && patientGuestId is not null))
            {
                throw new DomainException("Appointment must have either a registered patient or a guest");
            }

            if (slot.DoctorId != doctorId)
                throw new DomainException("Invalid doctor");

            if (slot.DayOfWeek != date.DayOfWeek)
                throw new DomainException("Slot does not match selected date");

            if (date < DateOnly.FromDateTime(DateTime.UtcNow))
                throw new DomainException("Invalid date");

            return new Appointment(
                doctorId,
                slot.Id,
                date,
                patientUserId,
                patientGuestId);
        }

        /// <summary>
        /// Reasigna una cita de invitado a un usuario registrado (merge guest → cuenta).
        /// Solo aplica a citas que pertenecen a un guest.
        /// </summary>
        public void ReassignToRegisteredUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new DomainException("userId requerido para reasignar la cita");
            if (PatientGuestId is null)
                throw new DomainException("Solo se pueden reasignar citas de invitados");

            PatientUserId = userId;
            PatientGuestId = null;
        }

        /// <summary>Soft-delete: marca esta cita como reagendada y registra el Id de la nueva.</summary>
        public void MarkAsRescheduled(Guid newAppointmentId)
        {
            Status = AppointmentStatus.Rescheduled;
            RescheduledToId = newAppointmentId;
        }

        /// <summary>Soft-delete: cancela la cita.</summary>
        public void Cancel()
        {
            Status = AppointmentStatus.Cancelled;
        }

        /// <summary>Marca la cita como completada (doctor la atendió).</summary>
        public void Complete()
        {
            if (Status != AppointmentStatus.Active)
                throw new DomainException("Solo las citas activas pueden marcarse como completadas.");
            Status = AppointmentStatus.Completed;
        }

        /// <summary>Marca la cita como inasistencia (paciente no se presentó).</summary>
        public void MarkAsNoShow()
        {
            if (Status != AppointmentStatus.Active)
                throw new DomainException("Solo las citas activas pueden marcarse como inasistencia.");
            Status = AppointmentStatus.NoShow;
        }
    }
}