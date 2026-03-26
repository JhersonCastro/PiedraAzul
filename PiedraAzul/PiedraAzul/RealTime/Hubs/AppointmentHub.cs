using Microsoft.AspNetCore.SignalR;
using PiedraAzul.Data.Cache;
using PiedraAzul.Shared.DTOs;
using PiedraAzul.Shared.RealTime.Contracts;
using System.Collections.Concurrent;

namespace PiedraAzul.RealTime.Hubs;

public class LockedSlotInfo
{
    public string SlotId { get; set; } = "";
    public string LockedBy { get; set; } = "";
    public DateTime LockExpiresAt { get; set; }
}

public class AppointmentHub : Hub<IAppointmentClient>
{
    private static readonly ConcurrentDictionary<string, (string DoctorId, DateTime Date, string SlotId)>
        _connectionSlots = new();

    private readonly ISlotCache _slotCache;
    private readonly ILogger<AppointmentHub> _logger;

    public AppointmentHub(ISlotCache slotCache, ILogger<AppointmentHub> logger)
    {
        _slotCache = slotCache;
        _logger = logger;
    }

    private static string GroupName(string doctorId, DateTime date)
        => $"{doctorId}:{date:yyyyMMdd}";

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Cliente desconectado: {ConnectionId}", Context.ConnectionId);

        if (_connectionSlots.TryRemove(Context.ConnectionId, out var info))
        {
            _logger.LogInformation("Liberando slot del cliente desconectado - DoctorId: {DoctorId}, SlotId: {SlotId}",
                info.DoctorId, info.SlotId);

            _slotCache.ReleaseSlot(info.DoctorId, info.Date, info.SlotId, Context.ConnectionId);

            await Clients.Group(GroupName(info.DoctorId, info.Date))
                .SlotReleased(info.SlotId, info.Date);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task<List<LockedSlotInfo>> GetLockedSlots(string doctorId, DateTime date)
    {
        _logger.LogInformation("GetLockedSlots - DoctorId: {DoctorId}, Date: {Date}", doctorId, date);

        var slots = _slotCache.GetSlots(doctorId, date);
        var lockedSlots = slots
            .Where(s => s.IsLocked)
            .Select(s => new LockedSlotInfo
            {
                SlotId = s.SlotId,
                LockedBy = s.LockedBy ?? "",
                LockExpiresAt = s.LockExpiresAt ?? DateTime.UtcNow
            })
            .ToList();

        _logger.LogInformation("GetLockedSlots - Encontrados {Count} slots bloqueados", lockedSlots.Count);
        return lockedSlots;
    }

    public async Task JoinGroup(string doctorId, DateTime date)
    {
        var group = GroupName(doctorId, date);
        await Groups.AddToGroupAsync(Context.ConnectionId, group);

        _logger.LogInformation("Cliente {ConnectionId} unido al grupo {Group}", Context.ConnectionId, group);

        // Limpiar slots expirados y notificar
        var expired = _slotCache.ExpireSlots(doctorId, date);
        foreach (var slot in expired)
        {
            _logger.LogInformation("Notificando slot expirado: {SlotId}", slot.SlotId);
            await Clients.Group(group).SlotReleased(slot.SlotId, date);
        }

        // Enviar estado actual de todos los slots al cliente que se une
        var slots = _slotCache.GetSlots(doctorId, date);
        _logger.LogInformation("Enviando {Count} slots al cliente {ConnectionId}", slots.Count, Context.ConnectionId);
        await Clients.Caller.HydrateSlots(slots);
    }

    public async Task<bool> SetLocked(string doctorId, string slotId, DateTime date)
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("SetLocked - DoctorId: {DoctorId}, SlotId: {SlotId}, Date: {Date}, ConnectionId: {ConnectionId}",
            doctorId, slotId, date, connectionId);

        // Liberar lock anterior si cambia
        if (_connectionSlots.TryGetValue(connectionId, out var currentLock))
        {
            var sameTarget = currentLock.DoctorId == doctorId &&
                currentLock.Date.Date == date.Date &&
                currentLock.SlotId == slotId;

            if (!sameTarget)
            {
                _logger.LogInformation("Liberando lock anterior - DoctorId: {DoctorId}, SlotId: {SlotId}",
                    currentLock.DoctorId, currentLock.SlotId);

                _slotCache.ReleaseSlot(currentLock.DoctorId, currentLock.Date, currentLock.SlotId, connectionId);

                await Clients.Group(GroupName(currentLock.DoctorId, currentLock.Date))
                    .SlotReleased(currentLock.SlotId, currentLock.Date);
            }
        }

        var success = _slotCache.TryLockSlot(doctorId, date, slotId, connectionId);
        _logger.LogInformation("TryLockSlot resultado: {Success}", success);

        if (!success)
        {
            // Obtener quién tiene el lock actualmente
            var slots = _slotCache.GetSlots(doctorId, date);
            var target = slots.FirstOrDefault(s => s.SlotId == slotId);
            var lockedBy = target?.LockedBy ?? "";

            _logger.LogInformation("Lock falló - Slot bloqueado por: {LockedBy}", lockedBy);
            await Clients.Caller.SlotLocked(slotId, date, lockedBy);
            return false;
        }

        _connectionSlots[connectionId] = (doctorId, date, slotId);
        _logger.LogInformation("Lock exitoso - Notificando al grupo");

        // Notificar a todos en el grupo
        await Clients.Group(GroupName(doctorId, date))
            .SlotLocked(slotId, date, connectionId);

        return true;
    }

    public async Task InitializeSlots(string doctorId, DateTime date, List<SlotStateDto> slots)
    {
        _logger.LogInformation("InitializeSlots - DoctorId: {DoctorId}, Date: {Date}, Slots: {Count}",
            doctorId, date, slots.Count);

        var existingSlots = _slotCache.GetSlots(doctorId, date);
        if (!existingSlots.Any())
        {
            _slotCache.SetSlots(doctorId, date, slots);
            _logger.LogInformation("Slots inicializados en caché");
        }
        else
        {
            _logger.LogInformation("Los slots ya existían en caché, no se sobrescriben");
        }
    }
    public async Task<bool> SetReleased(string doctorId, string slotId, DateTime date)
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("SetReleased - DoctorId: {DoctorId}, SlotId: {SlotId}, Date: {Date}, ConnectionId: {ConnectionId}",
            doctorId, slotId, date, connectionId);

        _slotCache.ReleaseSlot(doctorId, date, slotId, connectionId);

        if (_connectionSlots.TryGetValue(connectionId, out var currentLock) &&
            currentLock.DoctorId == doctorId &&
            currentLock.Date.Date == date.Date &&
            currentLock.SlotId == slotId)
        {
            _connectionSlots.TryRemove(connectionId, out _);
        }

        await Clients.Group(GroupName(doctorId, date))
            .SlotReleased(slotId, date);

        return true;
    }
}