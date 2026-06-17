namespace PiedraAzul.Client.Models.Schedule;

public class AvailabilityDayModel
{
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsEnabled { get; set; } = true;
    public TimeSpan StartTime { get; set; } = new(8, 0, 0);
    public TimeSpan EndTime { get; set; } = new(17, 0, 0);
}
