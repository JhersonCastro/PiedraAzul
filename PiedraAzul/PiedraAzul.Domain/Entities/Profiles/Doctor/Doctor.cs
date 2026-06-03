using PiedraAzul.Domain.Common.Exceptions;
using PiedraAzul.Domain.Entities.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Domain.Entities.Profiles.Doctor
{
    public class Doctor
    {
        public string Id { get; private set; }
        public DoctorType Specialty { get; private set; }
        public string LicenseNumber { get; private set; }
        public string Notes { get; private set; }
        public bool IsAvailable { get; private set; } = true;

        private readonly List<DoctorAvailabilitySlot> _slots = new();
        public IReadOnlyCollection<DoctorAvailabilitySlot> Slots => _slots;

        private Doctor() { }

        public Doctor(string userId, DoctorType specialty, string licenseNumber, string notes)
        {
            Id = userId;
            Specialty = specialty;
            LicenseNumber = licenseNumber;
            Notes = notes;
            IsAvailable = true;
        }

        public void SetAvailability(bool available) => IsAvailable = available;

        public void AddAvailability(DayOfWeek day, TimeSpan start, TimeSpan end)
        {
            if (start >= end)
                throw new DomainException("Invalid schedule");

            _slots.Add(new DoctorAvailabilitySlot(Id, day, start, end));
        }
    }
}
