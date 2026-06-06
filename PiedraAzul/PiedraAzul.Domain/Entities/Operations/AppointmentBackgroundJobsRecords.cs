using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Domain.Entities.Operations
{
    public class AppointmentBackgroundJobsRecords
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string JobId { get; set; } = default!;
        public Guid AppointmentId { get; set; }
    }
}
