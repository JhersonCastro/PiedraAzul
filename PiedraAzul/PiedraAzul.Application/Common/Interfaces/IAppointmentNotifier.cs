namespace PiedraAzul.Application.Common.Interfaces;

/// <summary>
/// Abstracción para notificar cambios de slots vía SignalR desde la capa Application.
/// La implementación concreta vive en la capa Infrastructure/Server.
/// </summary>
public interface IAppointmentNotifier
{
    Task NotifySlotReleasedAsync(string doctorId, string slotId, DateTime date);
}
