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

        private readonly List<DoctorAvailabilitySlot> _slots = new();
        public IReadOnlyCollection<DoctorAvailabilitySlot> Slots => _slots;

        private Doctor() { }

        public Doctor(string id, DoctorType specialty, string licenseNumber)
        {
            Id = id;
            Specialty = specialty;
            LicenseNumber = licenseNumber;
        }

        public void AddAvailability(DayOfWeek day, TimeSpan start, TimeSpan end)
        {
            if (start >= end)
                throw new DomainException("Invalid schedule");

            _slots.Add(new DoctorAvailabilitySlot(Id, day, start, end));
        }
    }
}
