using Mediator;
using PiedraAzul.Domain.Entities.Config;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Application.Features.ScheduleConfig.Commands.SaveScheduleConfig;

public class SaveScheduleConfigHandler(
    IDoctorScheduleConfigRepository doctorScheduleConfigRepository,
    ISystemConfigRepository systemConfigRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<SaveScheduleConfigCommand, bool>
{
    public async ValueTask<bool> Handle(SaveScheduleConfigCommand request, CancellationToken cancellationToken)
    {
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            var systemConfig = await systemConfigRepository.GetOrCreateAsync(ct);
            systemConfig.UpdateBookingWindowWeeks(request.BookingWindowWeeks);
            await systemConfigRepository.SaveAsync(systemConfig, ct);

            var dayEntities = request.Availability
                .Select(day => new DoctorScheduleDayConfig(day.DayOfWeek, day.IsEnabled, day.StartTime, day.EndTime))
                .ToList();

            var existingDoctorConfig = await doctorScheduleConfigRepository.GetByDoctorIdAsync(request.DoctorId, ct);
            if (existingDoctorConfig is null)
            {
                var newDoctorConfig = new DoctorScheduleConfig(request.DoctorId, request.IntervalMinutes, dayEntities);
                await doctorScheduleConfigRepository.AddAsync(newDoctorConfig, ct);
            }
            else
            {
                existingDoctorConfig.UpdateSlotInterval(request.IntervalMinutes);
                existingDoctorConfig.SetDays(dayEntities);
                await doctorScheduleConfigRepository.UpdateAsync(existingDoctorConfig, ct);
            }

            return true;
        }, cancellationToken);
    }
}
