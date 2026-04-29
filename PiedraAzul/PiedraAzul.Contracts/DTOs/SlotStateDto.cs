namespace PiedraAzul.Contracts.DTOs;

public class SlotStateDto
{
    public string SlotId { get; set; } = default!;
    public DateTime Start { get; set; }
    public bool IsReserved { get; set; }

    public bool IsLocked => LockedBy != null && LockExpiresAt > DateTime.UtcNow;

    public string? LockedBy { get; set; }
    public DateTime? LockExpiresAt { get; set; }
}