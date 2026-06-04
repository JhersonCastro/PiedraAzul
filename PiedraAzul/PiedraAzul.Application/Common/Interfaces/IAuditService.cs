namespace PiedraAzul.Application.Common.Interfaces;

/// <summary>
/// Registro de auditoría a nivel de negocio (acciones nombradas que el interceptor de EF
/// no puede capturar por sí solo, como login, OTP o merge).
/// </summary>
public interface IAuditService
{
    Task LogAsync(
        string entityType,
        string? entityId,
        string action,
        object? data = null,
        string? subjectIdentification = null,
        string? subjectName = null,
        string? subjectPhone = null,
        // Override del actor cuando se conoce y la petición no está autenticada
        // (ej. Login: quien ejecuta la acción es el propio usuario).
        string? actorUserId = null,
        string? actorName = null,
        string? actorRoles = null,
        CancellationToken cancellationToken = default);
}
