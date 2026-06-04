using System;

namespace PiedraAzul.Domain.Entities.Audit
{
    /// <summary>
    /// Registro de auditoría append-only. Captura qué entidad cambió, qué acción se hizo,
    /// quién la hizo y un payload JSON con el detalle. Campos de sujeto denormalizados
    /// (cédula/nombre/teléfono) para búsqueda rápida.
    /// </summary>
    public class AuditLog
    {
        public Guid Id { get; private set; }
        public DateTime Timestamp { get; private set; }

        /// <summary>Nombre de la entidad/recurso afectado (ej. "ApplicationUser", "Appointment").</summary>
        public string EntityType { get; private set; } = "";

        /// <summary>Identificador del registro afectado (PK), si aplica.</summary>
        public string? EntityId { get; private set; }

        /// <summary>Acción: Created/Updated/Deleted/Login/OtpSent/OtpVerified/GuestMerged/...</summary>
        public string Action { get; private set; } = "";

        /// <summary>"Interceptor" (cambio automático en BD) o "Business" (acción de negocio nombrada).</summary>
        public string Source { get; private set; } = "Interceptor";

        // ── Actor ──────────────────────────────────────────────
        public string? ActorUserId { get; private set; }
        public string? ActorName { get; private set; }
        public string? ActorRoles { get; private set; }
        public string? IpAddress { get; private set; }

        // ── Sujeto (denormalizado para búsqueda rápida) ─────────
        public string? SubjectIdentification { get; private set; }
        public string? SubjectName { get; private set; }
        public string? SubjectPhone { get; private set; }

        /// <summary>Payload JSON con el detalle del cambio (datos sensibles redactados).</summary>
        public string? Data { get; private set; }

        private AuditLog() { }

        public static AuditLog Create(
            string entityType,
            string? entityId,
            string action,
            string source,
            string? actorUserId,
            string? actorName,
            string? actorRoles,
            string? ipAddress,
            string? subjectIdentification,
            string? subjectName,
            string? subjectPhone,
            string? data)
        {
            return new AuditLog
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                Source = source,
                ActorUserId = actorUserId,
                ActorName = actorName,
                ActorRoles = actorRoles,
                IpAddress = ipAddress,
                SubjectIdentification = subjectIdentification,
                SubjectName = subjectName,
                SubjectPhone = subjectPhone,
                Data = data
            };
        }
    }
}
