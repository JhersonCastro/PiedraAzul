using PiedraAzul.Domain.Entities.Config;

namespace PiedraAzul.Domain.Repositories;

public interface IDoctorScheduleConfigRepository
{
    Task<DoctorScheduleConfig?> GetByDoctorIdAsync(string doctorId, CancellationToken cancellationToken = default);
    Task AddAsync(DoctorScheduleConfig config, CancellationToken cancellationToken = default);
    Task UpdateAsync(DoctorScheduleConfig config, CancellationToken cancellationToken = default);
}
