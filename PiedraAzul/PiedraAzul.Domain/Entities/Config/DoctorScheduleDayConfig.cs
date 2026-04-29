using PiedraAzul.Domain.Common.Exceptions;

namespace PiedraAzul.Domain.Entities.Config;

public class DoctorScheduleDayConfig
{
    public Guid Id { get; private set; }
    public Guid DoctorScheduleConfigId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public bool IsEnabled { get; private set; }
    public TimeSpan StartTime { get; private set; }
    public TimeSpan EndTime { get; private set; }

    private DoctorScheduleDayConfig() { }

    public DoctorScheduleDayConfig(DayOfWeek dayOfWeek, bool isEnabled, TimeSpan startTime, TimeSpan endTime)
    {
        if (isEnabled && startTime >= endTime)
            throw new DomainException("Rango horario inválido para el día.");

        Id = Guid.NewGuid();
        DayOfWeek = dayOfWeek;
        IsEnabled = isEnabled;
        StartTime = startTime;
        EndTime = endTime;
    }
}
