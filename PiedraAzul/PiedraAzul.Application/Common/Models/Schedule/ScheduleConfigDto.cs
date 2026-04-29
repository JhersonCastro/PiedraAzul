namespace PiedraAzul.Application.Common.Models.Schedule;

public record ScheduleConfigDto(
    string DoctorId,
    int BookingWindowWeeks,
    int IntervalMinutes,
    IReadOnlyList<ScheduleDayDto> Availability);
