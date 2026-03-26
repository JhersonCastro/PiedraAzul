using PiedraAzul.Shared.DTOs;

namespace PiedraAzul.Shared.RealTime.Contracts;

public interface IAppointmentClient
{
    Task HydrateSlots(List<SlotStateDto> slots);
    Task SlotLocked(string slotId, DateTime date, string lockedBy);
    Task SlotReleased(string slotId, DateTime date);
    Task IsExpired(string slotId, DateTime date);
}