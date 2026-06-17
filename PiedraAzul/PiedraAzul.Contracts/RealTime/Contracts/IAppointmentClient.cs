using PiedraAzul.Contracts.DTOs;

namespace PiedraAzul.Contracts.RealTime.Contracts;

public interface IAppointmentClient
{
    Task HydrateSlots(List<SlotStateDto> slots);
    Task SlotLocked(string slotId, DateTime date, string lockedBy);
    Task SlotReleased(string slotId, DateTime date);
    Task IsExpired(string slotId, DateTime date);
}