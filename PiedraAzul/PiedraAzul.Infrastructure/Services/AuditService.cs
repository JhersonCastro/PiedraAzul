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
        CancellationToken cancellationToken = default)
    {
        var (actorId, actorName, actorRoles, ip) = AuditActor.Resolve(httpContextAccessor);

        var entry = AuditLog.Create(
            entityType: entityType,
            entityId: entityId,
            action: action,
            source: "Business",
            actorUserId: actorId,
            actorName: actorName,
            actorRoles: actorRoles,
            ipAddress: ip,
            subjectIdentification: subjectIdentification,
            subjectName: subjectName,
            subjectPhone: subjectPhone,
            data: AuditData.Serialize(data));

        await context.AuditLogs.AddAsync(entry, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
