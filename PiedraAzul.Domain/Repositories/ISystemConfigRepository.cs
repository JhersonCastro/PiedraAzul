using PiedraAzul.Domain.Entities.Config;

namespace PiedraAzul.Domain.Repositories;

public interface ISystemConfigRepository
{
    Task<SystemConfig?> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(SystemConfig config, CancellationToken cancellationToken = default);
}