namespace PiedraAzul.Application.Common.Models.Schedule;

/// <summary>Slot de disponibilidad recurrente tal cual está guardado (incluye los borrados).</summary>
public record RawSlotDto(
    Guid Id,
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    bool IsDeleted);
