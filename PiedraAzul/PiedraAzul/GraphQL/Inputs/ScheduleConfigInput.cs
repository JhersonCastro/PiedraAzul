namespace PiedraAzul.GraphQL.Inputs;

public record ScheduleDayInput(
    DayOfWeek DayOfWeek,
    bool IsEnabled,
    TimeSpan StartTime,
    TimeSpan EndTime);

public record ScheduleConfigInput(
    string DoctorId,
    int BookingWindowWeeks,
    int IntervalMinutes,
    IReadOnlyList<ScheduleDayInput> Availability);
