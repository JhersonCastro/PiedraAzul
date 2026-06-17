using PiedraAzul.Application.Common.Models.Schedule;

namespace PiedraAzul.GraphQL.Types;

public class ScheduleDayType
{
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsEnabled { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class ScheduleConfigType
{
    public string DoctorId { get; set; } = string.Empty;
    public int BookingWindowWeeks { get; set; }
    public int IntervalMinutes { get; set; }
    public IReadOnlyList<ScheduleDayType> Availability { get; set; } = [];

    public static ScheduleConfigType FromDto(ScheduleConfigDto dto)
    {
        return new ScheduleConfigType
        {
            DoctorId = dto.DoctorId,
            BookingWindowWeeks = dto.BookingWindowWeeks,
            IntervalMinutes = dto.IntervalMinutes,
            Availability = dto.Availability
                .Select(day => new ScheduleDayType
                {
                    DayOfWeek = day.DayOfWeek,
                    IsEnabled = day.IsEnabled,
                    StartTime = day.StartTime,
                    EndTime = day.EndTime
                })
                .ToList()
        };
    }
}
