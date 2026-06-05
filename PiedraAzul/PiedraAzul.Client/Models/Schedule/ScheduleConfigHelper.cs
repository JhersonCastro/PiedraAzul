namespace PiedraAzul.Client.Models.Schedule;

/// <summary>
/// Utilidades compartidas para inferir la franja horaria y el intervalo
/// de una configuración de horario desde sus slots activos guardados.
/// Usada tanto por el portal del doctor (DoctorScheduleConfig)
/// como por el admin (AdminScheduleConfig).
/// </summary>
public static class ScheduleConfigHelper
{
    /// <summary>
    /// Detecta el intervalo de atención real desde los slots guardados.
    /// Toma cualquier slot activo (!IsDeleted) y calcula EndTime − StartTime.
    /// </summary>
    public static int DetectInterval(ScheduleConfigModel config)
    {
        var active = config.Slots?.FirstOrDefault(s => !s.IsDeleted);
        if (active is not null
            && TimeSpan.TryParse(active.StartTime, out var st)
            && TimeSpan.TryParse(active.EndTime,   out var et))
        {
            var mins = (int)(et - st).TotalMinutes;
            if (mins >= 5 && mins <= 60) return mins;
        }

        return config.IntervalMinutes > 0 ? config.IntervalMinutes : 15;
    }

    /// <summary>
    /// Infiere la franja horaria global de la configuración:
    /// apertura = mínimo StartTime de todos los slots activos (cualquier día),
    /// cierre   = máximo EndTime   de todos los slots activos (cualquier día).
    ///
    /// Si no hay slots activos, usa el primer día habilitado en Availability
    /// como fallback (comportamiento anterior).
    /// </summary>
    public static (TimeSpan Start, TimeSpan End) DetectTimeRange(ScheduleConfigModel config)
    {
        var activeSlots = config.Slots?.Where(s => !s.IsDeleted).ToList() ?? new();

        if (activeSlots.Count > 0)
        {
            var minStart = activeSlots
                .Where(s => TimeSpan.TryParse(s.StartTime, out _))
                .Select(s => TimeSpan.Parse(s.StartTime))
                .DefaultIfEmpty(new TimeSpan(8, 0, 0))
                .Min();

            var maxEnd = activeSlots
                .Where(s => TimeSpan.TryParse(s.EndTime, out _))
                .Select(s => TimeSpan.Parse(s.EndTime))
                .DefaultIfEmpty(new TimeSpan(17, 0, 0))
                .Max();

            return (minStart, maxEnd);
        }

        // Fallback: primer día habilitado
        var sample = config.Availability?.FirstOrDefault(a => a.IsEnabled)
                  ?? config.Availability?.FirstOrDefault();

        return sample is null
            ? (new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0))
            : (sample.StartTimeSpan, sample.EndTimeSpan);
    }
}
