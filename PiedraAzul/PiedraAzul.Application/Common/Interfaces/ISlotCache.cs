using PiedraAzul.Contracts.DTOs;

namespace PiedraAzul.Application.Common.Interfaces;

public interface ISlotCache
{
    List<SlotStateDto> GetSlots(string doctorId, DateTime date);
    void SetSlots(string doctorId, DateTime date, List<SlotStateDto> slots);
    bool TryLockSlot(string doctorId, DateTime date, string slotId, string connectionId);
    void ReleaseSlot(string doctorId, DateTime date, string slotId, string? connectionId);
    List<SlotStateDto> ExpireSlots(string doctorId, DateTime date);
}