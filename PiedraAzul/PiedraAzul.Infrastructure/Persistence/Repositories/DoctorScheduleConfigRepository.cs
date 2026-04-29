using Microsoft.EntityFrameworkCore;
using PiedraAzul.Domain.Entities.Config;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Infrastructure.Persistence.Repositories;

public class DoctorScheduleConfigRepository(AppDbContext context) : IDoctorScheduleConfigRepository
{
    public async Task<DoctorScheduleConfig?> GetByDoctorIdAsync(string doctorId, CancellationToken cancellationToken = default)
    {
        return await context.Set<DoctorScheduleConfig>()
            .Include(x => x.Days)
            .FirstOrDefaultAsync(x => x.DoctorId == doctorId, cancellationToken);
    }

    public async Task AddAsync(DoctorScheduleConfig config, CancellationToken cancellationToken = default)
    {
        await context.Set<DoctorScheduleConfig>().AddAsync(config, cancellationToken);
    }

    public Task UpdateAsync(DoctorScheduleConfig config, CancellationToken cancellationToken = default)
    {
        context.Set<DoctorScheduleConfig>().Update(config);
        return Task.CompletedTask;
    }
}
