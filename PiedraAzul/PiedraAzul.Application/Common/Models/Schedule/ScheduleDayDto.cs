namespace PiedraAzul.Application.Common.Models.Schedule;

public record ScheduleDayDto(
    DayOfWeek DayOfWeek,
    bool IsEnabled,
    TimeSpan StartTime,
    TimeSpan EndTime);
