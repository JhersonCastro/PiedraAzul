using Mediator;
using PiedraAzul.Application.Common.Models.Schedule;
using PiedraAzul.Domain.Entities.Config;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Application.Features.ScheduleConfig.Queries.GetScheduleConfig;

public class GetScheduleConfigHandler(
    IDoctorScheduleConfigRepository doctorScheduleConfigRepository,
    ISystemConfigRepository systemConfigRepository) : IRequestHandler<GetScheduleConfigQuery, ScheduleConfigDto>
{
    public async ValueTask<ScheduleConfigDto> Handle(GetScheduleConfigQuery request, CancellationToken cancellationToken)
    {
        var systemConfig = await systemConfigRepository.GetOrCreateAsync(cancellationToken);
        var doctorConfig = await doctorScheduleConfigRepository.GetByDoctorIdAsync(request.DoctorId, cancellationToken);

        var days = doctorConfig?.Days
            .OrderBy(x => x.DayOfWeek)
            .Select(x => new ScheduleDayDto(x.DayOfWeek, x.IsEnabled, x.StartTime, x.EndTime))
            .ToList()
            ?? BuildDefaultDays();

        return new ScheduleConfigDto(
            request.DoctorId,
            systemConfig.BookingWindowWeeks,
            doctorConfig?.SlotIntervalMinutes ?? 15,
            days);
    }

    private static List<ScheduleDayDto> BuildDefaultDays()
    {
        var weekdays = new[]
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday
        };

        return weekdays
            .Select(day => new ScheduleDayDto(day, true, new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0)))
            .ToList();
    }
}
