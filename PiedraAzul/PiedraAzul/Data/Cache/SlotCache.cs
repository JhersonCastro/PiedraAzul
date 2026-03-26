using Microsoft.Extensions.Caching.Memory;
using PiedraAzul.Shared.DTOs;

namespace PiedraAzul.Data.Cache
{
    public interface ISlotCache
    {
        List<SlotStateDto> GetSlots(string doctorId, DateTime date);
        void SetSlots(string doctorId, DateTime date, List<SlotStateDto> slots);
        bool TryLockSlot(string doctorId, DateTime date, string slotId, string connectionId);
        void ReleaseSlot(string doctorId, DateTime date, string slotId, string? connectionId);
        List<SlotStateDto> ExpireSlots(string doctorId, DateTime date);
    }

    public class SlotCache : ISlotCache
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<SlotCache> _logger;
        private static readonly object _lockObject = new object();

        public SlotCache(IMemoryCache cache, ILogger<SlotCache> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        private string GetKey(string doctorId, DateTime date)
            => $"slots:{doctorId}:{date:yyyyMMdd}";

        public List<SlotStateDto> GetSlots(string doctorId, DateTime date)
        {
            var slots = _cache.Get<List<SlotStateDto>>(GetKey(doctorId, date));
            _logger.LogInformation("GetSlots - DoctorId: {DoctorId}, Date: {Date}, Slots count: {Count}",
                doctorId, date, slots?.Count ?? 0);
            return slots ?? new List<SlotStateDto>();
        }

        public void SetSlots(string doctorId, DateTime date, List<SlotStateDto> slots)
        {
            _logger.LogInformation("SetSlots - DoctorId: {DoctorId}, Date: {Date}, Slots count: {Count}",
                doctorId, date, slots.Count);
            _cache.Set(GetKey(doctorId, date), slots, TimeSpan.FromHours(12));
        }

        public bool TryLockSlot(string doctorId, DateTime date, string slotId, string connectionId)
        {
            lock (_lockObject)
            {
                _logger.LogInformation("TryLockSlot - DoctorId: {DoctorId}, SlotId: {SlotId}, ConnectionId: {ConnectionId}",
                    doctorId, slotId, connectionId);

                var slots = GetSlots(doctorId, date);

                if (!slots.Any())
                {
                    _logger.LogWarning("No hay slots en caché para DoctorId: {DoctorId}, Date: {Date}", doctorId, date);
                    return false;
                }

                var target = slots.FirstOrDefault(s => s.SlotId == slotId);

                if (target == null)
                {
                    _logger.LogWarning("Slot no encontrado: {SlotId}", slotId);
                    return false;
                }

                _logger.LogInformation("Slot encontrado - IsReserved: {IsReserved}, LockedBy: {LockedBy}, LockExpiresAt: {LockExpiresAt}",
                    target.IsReserved, target.LockedBy, target.LockExpiresAt);

                // Si ya está reservado permanentemente, no se puede bloquear
                if (target.IsReserved)
                {
                    _logger.LogWarning("Slot ya está reservado permanentemente");
                    return false;
                }

                // Si ya está bloqueado por otro y no ha expirado
                if (target.LockedBy != null &&
                    target.LockedBy != connectionId &&
                    target.LockExpiresAt > DateTime.UtcNow)
                {
                    _logger.LogWarning("Slot ya está bloqueado por otro usuario: {LockedBy}", target.LockedBy);
                    return false;
                }

                // Bloquear el slot
                target.LockedBy = connectionId;
                target.LockExpiresAt = DateTime.UtcNow.AddMinutes(5);

                _logger.LogInformation("Slot bloqueado exitosamente - LockedBy: {LockedBy}, ExpiresAt: {ExpiresAt}",
                    target.LockedBy, target.LockExpiresAt);

                SetSlots(doctorId, date, slots);
                return true;
            }
        }

        public void ReleaseSlot(string doctorId, DateTime date, string slotId, string? connectionId)
        {
            _logger.LogInformation("ReleaseSlot - DoctorId: {DoctorId}, SlotId: {SlotId}, ConnectionId: {ConnectionId}",
                doctorId, slotId, connectionId);

            var slots = GetSlots(doctorId, date);
            var target = slots.FirstOrDefault(s => s.SlotId == slotId);

            if (target == null) return;

            // Solo el dueño puede liberarlo (o si ya expiró)
            if (target.LockedBy != connectionId && target.LockExpiresAt > DateTime.UtcNow)
            {
                _logger.LogWarning("No se puede liberar - No es el dueño. LockedBy: {LockedBy}, Current: {Current}",
                    target.LockedBy, connectionId);
                return;
            }

            target.LockedBy = null;
            target.LockExpiresAt = null;

            _logger.LogInformation("Slot liberado exitosamente");
            SetSlots(doctorId, date, slots);
        }

        public List<SlotStateDto> ExpireSlots(string doctorId, DateTime date)
        {
            lock (_lockObject)
            {
                var slots = GetSlots(doctorId, date);
                var expired = slots
                    .Where(s => s.LockedBy != null && s.LockExpiresAt <= DateTime.UtcNow)
                    .ToList();

                _logger.LogInformation("ExpireSlots - DoctorId: {DoctorId}, Date: {Date}, Expired slots: {Count}",
                    doctorId, date, expired.Count);

                foreach (var s in expired)
                {
                    _logger.LogInformation("Liberando slot expirado: {SlotId}, LockedBy: {LockedBy}", s.SlotId, s.LockedBy);
                    s.LockedBy = null;
                    s.LockExpiresAt = null;
                }

                if (expired.Any())
                    SetSlots(doctorId, date, slots);

                return expired;
            }
        }
    }
}