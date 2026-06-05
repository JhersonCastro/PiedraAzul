using Mediator;
using PiedraAzul.Application.Common.Caching;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Notifications;

namespace PiedraAzul.Application.Features.Appointments.Notifications;

/// <summary>
/// Invalida el cache de lectura del doctor (slots del día + lista de días disponibles) cuando
/// una cita cambia de estado. En un reagendamiento refresca tanto el día nuevo como el de origen.
/// <para>
/// La invalidación es una consecuencia del cambio de dominio, no del canal de entrada: por eso
/// vive aquí como handler del evento y no en el resolver de GraphQL. Así ocurre siempre, sin
/// importar quién dispare el comando (GraphQL, REST, un job, otro comando que lo reutilice).
/// </para>
/// </summary>
public sealed class AppointmentCacheInvalidationHandler : INotificationHandler<AppointmentNotification>
{
    private readonly ICacheService _cache;

    public AppointmentCacheInvalidationHandler(ICacheService cache) => _cache = cache;

    public ValueTask Handle(AppointmentNotification n, CancellationToken ct)
    {
        InvalidateDoctorDay(n.DoctorId, n.Date);

        // Reagendamiento: el día/doctor de origen también deben refrescarse.
        if (n.OldDoctorId is not null && n.OldDate is not null)
            InvalidateDoctorDay(n.OldDoctorId, n.OldDate.Value);

        return default;
    }

    private void InvalidateDoctorDay(string doctorId, DateOnly date)
    {
        _cache.Remove(CacheKeys.DoctorDaySlots(doctorId, date));
        _cache.RemoveByTag(CacheKeys.TagDoctorDays(doctorId));
    }
}
