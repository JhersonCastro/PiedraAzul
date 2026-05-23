using Microsoft.AspNetCore.SignalR;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Contracts.RealTime.Contracts;
using PiedraAzul.RealTime.Hubs;

namespace PiedraAzul.RealTime;

/// <summary>
/// Implementación de IAppointmentNotifier que usa SignalR para notificar clientes.
/// </summary>
public class AppointmentNotifier(IHubContext<AppointmentHub, IAppointmentClient> hub)
    : IAppointmentNotifier
{
    public async Task NotifySlotReleasedAsync(string doctorId, string slotId, DateTime date)
    {
        var group = $"{doctorId}:{date:yyyyMMdd}";
        await hub.Clients.Group(group).SlotReleased(slotId, date);
    }
}
