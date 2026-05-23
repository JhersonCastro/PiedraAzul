using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Application.Common.Models.Appointments
{
    public class AppointmentDto
    {
        public Guid Id { get; set; }

        public string? PatientUserId { get; set; }
        public string? PatientGuestId { get; set; }

        public string PatientType { get; set; } = default!;
        public string PatientName { get; set; } = default!;

        public Guid SlotId { get; set; }

        public string DoctorId { get; set; } = default!;
        public string DoctorName { get; set; } = default!;
        public string Specialty { get; set; } = default!;

        public DateTime Start { get; set; }

        public DateTime CreatedAt { get; set; }

        public string Status { get; set; } = "Active";
    }
}
