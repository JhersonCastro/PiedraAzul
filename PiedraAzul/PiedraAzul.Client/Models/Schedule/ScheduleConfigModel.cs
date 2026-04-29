using System.ComponentModel.DataAnnotations;

namespace PiedraAzul.Client.Models.Schedule;

public class ScheduleConfigModel
{
    [Required(ErrorMessage = "Selecciona un especialista.")]
    public string DoctorId { get; set; } = "";

    [Obsolete("Use DoctorId instead. This alias will be removed in a future version.")]
    public string SpecialistId
    {
        get => DoctorId;
        set => DoctorId = value;
    }

    [Range(1, 52, ErrorMessage = "La ventana de semanas debe ser entre 1 y 52.")]
    public int BookingWindowWeeks { get; set; } = 4;

    [Obsolete("Use BookingWindowWeeks instead. This alias will be removed in a future version.")]
    public int WeekWindowInWeeks
    {
        get => BookingWindowWeeks;
        set => BookingWindowWeeks = value;
    }

    [Range(5, 60, ErrorMessage = "El intervalo debe ser entre 5 y 60 minutos.")]
    public int IntervalMinutes { get; set; } = 15;

    public List<AvailabilityDayModel> Availability { get; set; } = [];

    public string StartTime
    {
        get
        {
            var day = Availability.FirstOrDefault(x => x.IsEnabled) ?? Availability.FirstOrDefault();
            return day is null ? "08:00" : $"{day.StartTime.Hours:00}:{day.StartTime.Minutes:00}";
        }
        set => ApplyTimeToAllDays(value, isStart: true);
    }

    public string EndTime
    {
        get
        {
            var day = Availability.FirstOrDefault(x => x.IsEnabled) ?? Availability.FirstOrDefault();
            return day is null ? "17:00" : $"{day.EndTime.Hours:00}:{day.EndTime.Minutes:00}";
        }
        set => ApplyTimeToAllDays(value, isStart: false);
    }

    public bool MondayEnabled
    {
        get => IsEnabled(DayOfWeek.Monday);
        set => SetEnabled(DayOfWeek.Monday, value);
    }

    public bool TuesdayEnabled
    {
        get => IsEnabled(DayOfWeek.Tuesday);
        set => SetEnabled(DayOfWeek.Tuesday, value);
    }

    public bool WednesdayEnabled
    {
        get => IsEnabled(DayOfWeek.Wednesday);
        set => SetEnabled(DayOfWeek.Wednesday, value);
    }

    public bool ThursdayEnabled
    {
        get => IsEnabled(DayOfWeek.Thursday);
        set => SetEnabled(DayOfWeek.Thursday, value);
    }

    public bool FridayEnabled
    {
        get => IsEnabled(DayOfWeek.Friday);
        set => SetEnabled(DayOfWeek.Friday, value);
    }

    private bool IsEnabled(DayOfWeek day) => Availability.FirstOrDefault(x => x.DayOfWeek == day)?.IsEnabled ?? false;

    private void SetEnabled(DayOfWeek day, bool isEnabled)
    {
        var item = Availability.FirstOrDefault(x => x.DayOfWeek == day);
        if (item is not null)
        {
            item.IsEnabled = isEnabled;
        }
    }

    private void ApplyTimeToAllDays(string value, bool isStart)
    {
        if (!TimeSpan.TryParse(value, out var parsed))
        {
            return;
        }

        foreach (var day in Availability)
        {
            if (isStart)
            {
                day.StartTime = parsed;
            }
            else
            {
                day.EndTime = parsed;
            }
        }
    }
}
