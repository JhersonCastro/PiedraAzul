using PiedraAzul.Client.Models.Schedule;

namespace PiedraAzul.Client.Services.AdminServices;

public class ScheduleConfigAdminService
{
    private readonly Dictionary<string, ScheduleConfigEditModel> _store = new();

    public Task<ScheduleConfigEditModel> GetBySpecialistAsync(string specialistId)
    {
        if (string.IsNullOrWhiteSpace(specialistId))
        {
            return Task.FromResult(new ScheduleConfigEditModel());
        }

        if (!_store.TryGetValue(specialistId, out var config))
        {
            config = BuildDefaultConfig(specialistId);
            _store[specialistId] = Clone(config);
        }

        return Task.FromResult(Clone(config));
    }

    public async Task SaveAsync(ScheduleConfigEditModel model, CancellationToken cancellationToken = default)
    {
        await Task.Delay(450, cancellationToken);
        _store[model.DoctorId] = Clone(model);
    }

    private static ScheduleConfigEditModel BuildDefaultConfig(string specialistId)
    {
        var hash = Math.Abs(specialistId.GetHashCode());
        var startHour = 7 + (hash % 3);
        var endHour = 16 + (hash % 4);
        var interval = new[] { 10, 15, 20 }[hash % 3];

        return new ScheduleConfigEditModel
        {
            DoctorId = specialistId,
            BookingWindowWeeks = 2 + (hash % 3),
            IntervalMinutes = interval,
            Availability =
            [
                new() { DayOfWeek = DayOfWeek.Monday, IsEnabled = true, StartTime = new(startHour, 0, 0), EndTime = new(endHour, 0, 0) },
                new() { DayOfWeek = DayOfWeek.Tuesday, IsEnabled = true, StartTime = new(startHour, 0, 0), EndTime = new(endHour, 0, 0) },
                new() { DayOfWeek = DayOfWeek.Wednesday, IsEnabled = hash % 2 == 0, StartTime = new(startHour, 0, 0), EndTime = new(endHour, 0, 0) },
                new() { DayOfWeek = DayOfWeek.Thursday, IsEnabled = true, StartTime = new(startHour, 0, 0), EndTime = new(endHour, 0, 0) },
                new() { DayOfWeek = DayOfWeek.Friday, IsEnabled = hash % 3 != 0, StartTime = new(startHour, 0, 0), EndTime = new(endHour, 0, 0) }
            ]
        };
    }

    private static ScheduleConfigEditModel Clone(ScheduleConfigEditModel model) => new()
    {
        DoctorId = model.DoctorId,
        BookingWindowWeeks = model.BookingWindowWeeks,
        IntervalMinutes = model.IntervalMinutes,
        Availability = model.Availability
            .Select(day => new AvailabilityDayModel
            {
                DayOfWeek = day.DayOfWeek,
                IsEnabled = day.IsEnabled,
                StartTime = day.StartTime,
                EndTime = day.EndTime
            })
            .ToList()
    };
}
