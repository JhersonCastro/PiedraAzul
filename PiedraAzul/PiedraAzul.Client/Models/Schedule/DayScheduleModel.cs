namespace PiedraAzul.Client.Models.Schedule;

public class DayScheduleModel
{
    public string DayName { get; set; } = string.Empty;
    public int DayNumber { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsBlocked { get; set; }
    public List<TimeSlotModel> Slots { get; set; } = [];
}
