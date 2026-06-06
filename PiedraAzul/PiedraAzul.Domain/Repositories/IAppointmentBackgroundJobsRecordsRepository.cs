using PiedraAzul.Domain.Entities.Operations;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Domain.Repositories
{
    public interface IAppointmentBackgroundJobsRecordsRepository
    {
        Task<string[]> GetJobsIdsByAppointmentId(Guid appointmentId, CancellationToken ct);
        Task AddJobRecord(AppointmentBackgroundJobsRecords record, CancellationToken ct);
        Task AddJobsRecords(List<AppointmentBackgroundJobsRecords> records, CancellationToken ct);
    }
}
