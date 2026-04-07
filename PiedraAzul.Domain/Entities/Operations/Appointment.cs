using PiedraAzul.Domain.Common.Exceptions;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Domain.Entities.Operations
{
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
            if (date < DateOnly.FromDateTime(DateTime.UtcNow))
                throw new DomainException("Invalid date");

            if (!slot.Matches(date))
                throw new DomainException("Invalid slot");

            if (slot.DoctorId != doctorId)
                throw new DomainException("Invalid doctor");

            return new Appointment(
                doctorId,
                slot.Id,
                date,
                patientUserId,
                patientGuestId);
        }
    }
}
