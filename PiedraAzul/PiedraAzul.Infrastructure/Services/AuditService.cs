using Microsoft.AspNetCore.Http;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Domain.Entities.Audit;
using PiedraAzul.Infrastructure.Audit;
using PiedraAzul.Infrastructure.Persistence;

namespace PiedraAzul.Infrastructure.Services;

/// <summary>Auditoría a nivel de negocio (acciones nombradas).</summary>
public class AuditService(AppDbContext context, IHttpContextAccessor httpContextAccessor) : IAuditService
{
    public async Task LogAsync(
        string entityType,
        string? entityId,
        string action,
        object? data = null,
        string? subjectIdentification = null,
        string? subjectName = null,
        string? subjectPhone = null,
        string? actorUserId = null,
        string? actorName = null,
        string? actorRoles = null,
        CancellationToken cancellationToken = default)
    {
        var (resolvedId, resolvedName, resolvedRoles, ip) = AuditActor.Resolve(httpContextAccessor);

        var entry = AuditLog.Create(
            entityType: entityType,
            entityId: entityId,
            action: action,
            source: "Business",
            actorUserId: actorUserId ?? resolvedId,
            actorName: actorName ?? resolvedName,
            actorRoles: actorRoles ?? resolvedRoles,
            ipAddress: ip,
            subjectIdentification: subjectIdentification,
            subjectName: subjectName,
            subjectPhone: subjectPhone,
            data: AuditData.Serialize(data));

        await context.AuditLogs.AddAsync(entry, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
