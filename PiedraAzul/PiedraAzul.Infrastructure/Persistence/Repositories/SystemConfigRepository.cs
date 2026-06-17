using Microsoft.EntityFrameworkCore;
using PiedraAzul.Domain.Entities.Config;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Infrastructure.Persistence.Repositories;

public class SystemConfigRepository(AppDbContext context) : ISystemConfigRepository
{
    public async Task<SystemConfig?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        return await context.SystemConfigs
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SystemConfig> GetOrCreateAsync(CancellationToken cancellationToken = default)
    {
        var existing = await GetCurrentAsync(cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var created = new SystemConfig(4);
        await context.SystemConfigs.AddAsync(created, cancellationToken);
        return created;
    }

    public Task SaveAsync(SystemConfig config, CancellationToken cancellationToken = default)
    {
        if (context.Entry(config).State == EntityState.Detached)
        {
            context.SystemConfigs.Update(config);
        }

        return Task.CompletedTask;
    }
}
