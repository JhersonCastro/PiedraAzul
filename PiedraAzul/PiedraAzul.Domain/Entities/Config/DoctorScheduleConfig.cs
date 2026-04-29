using PiedraAzul.Domain.Common.Exceptions;

namespace PiedraAzul.Domain.Entities.Config;

public class DoctorScheduleConfig
{
    private readonly List<DoctorScheduleDayConfig> _days = [];

    public Guid Id { get; private set; }
    public string DoctorId { get; private set; } = string.Empty;
    public int SlotIntervalMinutes { get; private set; }
    public IReadOnlyCollection<DoctorScheduleDayConfig> Days => _days;

    private DoctorScheduleConfig() { }

    public DoctorScheduleConfig(string doctorId, int slotIntervalMinutes, IEnumerable<DoctorScheduleDayConfig>? days = null)
    {
        Id = Guid.NewGuid();
        DoctorId = doctorId;
        UpdateSlotInterval(slotIntervalMinutes);
        SetDays(days ?? []);
    }

    public void UpdateSlotInterval(int slotIntervalMinutes)
    {
        if (slotIntervalMinutes < 5 || slotIntervalMinutes > 60)
            throw new DomainException("Intervalo inválido. Debe estar entre 5 y 60 minutos.");

        SlotIntervalMinutes = slotIntervalMinutes;
    }

    public void SetDays(IEnumerable<DoctorScheduleDayConfig> days)
    {
        var normalized = days
            .GroupBy(x => x.DayOfWeek)
            .Select(x => x.First())
            .OrderBy(x => x.DayOfWeek)
            .ToList();

        if (normalized.Count == 0)
            throw new DomainException("Debe existir configuración por al menos un día.");

        _days.Clear();
        _days.AddRange(normalized);
    }
}
