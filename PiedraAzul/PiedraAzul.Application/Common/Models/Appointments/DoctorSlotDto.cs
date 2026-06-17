using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Application.Common.Models.Appointments
{
    public class DoctorSlotDto
    {
        public Guid SlotId { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsAvailable { get; set; }
    }
}
