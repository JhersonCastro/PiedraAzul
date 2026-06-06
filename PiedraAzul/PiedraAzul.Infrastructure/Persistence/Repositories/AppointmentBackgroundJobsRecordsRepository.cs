using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
    
namespace PiedraAzul.Infrastructure.Persistence.Repositories
{
    public class AppointmentBackgroundJobsRecordsRepository : IAppointmentBackgroundJobsRecordsRepository
    {
        private readonly AppDbContext _context;

        public AppointmentBackgroundJobsRecordsRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task AddJobRecord(AppointmentBackgroundJobsRecords record, CancellationToken ct)
        {
            await _context.AddAsync(record, ct);
        }

        public async Task AddJobsRecords(List<AppointmentBackgroundJobsRecords> records, CancellationToken ct)
        {
            await _context.AddRangeAsync(records, ct);
        }

        public async Task<string[]> GetJobsIdsByAppointmentId(Guid appointmentId, CancellationToken ct)
        {
            var records = await _context.appointmentBackgroundJobsRecords
                                .Where(r => r.AppointmentId == appointmentId).Select(r => r.JobId).ToArrayAsync(ct);
            return records;
        }
    }
}
