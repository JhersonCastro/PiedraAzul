using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PiedraAzul.Domain.Entities.Audit;
using PiedraAzul.Domain.Entities.Profiles.Patients;
using PiedraAzul.Infrastructure.Identity;

namespace PiedraAzul.Infrastructure.Audit;

/// <summary>
/// Captura automáticamente todo INSERT/UPDATE/DELETE de entidades relevantes y registra
/// un AuditLog con el detalle (datos sensibles redactados). Cobertura "de todo" sin
/// tener que instrumentar cada mutación.
/// </summary>
public sealed class AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    private static readonly HashSet<string> SkipTypes = new()
    {
        nameof(AuditLog), "DataProtectionKey", "RefreshToken", "GuestVerificationSession"
    };

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AddAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        AddAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddAuditEntries(DbContext? context)
    {
        if (context is null) return;

        // Materializar antes de agregar nuevas filas de auditoría.
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => ShouldAudit(e.Entity.GetType()))
            .ToList();

        if (entries.Count == 0) return;

        var (actorId, actorName, actorRoles, ip) = AuditActor.Resolve(httpContextAccessor);

        foreach (var entry in entries)
        {
            var (action, data) = Describe(entry);
            if (action is null) continue;

            var (subjId, subjName, subjPhone) = ResolveSubject(entry.Entity);

            var log = AuditLog.Create(
                entityType: entry.Entity.GetType().Name,
                entityId: GetPrimaryKey(entry),
                action: action,
                source: "Interceptor",
                actorUserId: actorId,
                actorName: actorName,
                actorRoles: actorRoles,
                ipAddress: ip,
                subjectIdentification: subjId,
                subjectName: subjName,
                subjectPhone: subjPhone,
                data: data);

            context.Set<AuditLog>().Add(log);
        }
    }

    private static bool ShouldAudit(Type type)
    {
        if (SkipTypes.Contains(type.Name)) return false;
        var ns = type.Namespace ?? "";
        // Tablas de join de Identity (IdentityUserRole, IdentityUserClaim, etc.). ApplicationUser
        // vive en PiedraAzul.Infrastructure.Identity, así que sí se audita.
        if (ns.StartsWith("Microsoft.AspNetCore.Identity")) return false;
        return true;
    }

    private static (string? action, string? data) Describe(EntityEntry entry)
    {
        switch (entry.State)
        {
            case EntityState.Added:
            {
                var dict = entry.Properties
                    .Where(p => !AuditData.IsSensitive(p.Metadata.Name))
                    .ToDictionary(p => p.Metadata.Name, p => Safe(p.CurrentValue));
                return ("Created", AuditData.Serialize(dict));
            }
            case EntityState.Deleted:
            {
                var dict = entry.Properties
                    .Where(p => !AuditData.IsSensitive(p.Metadata.Name))
                    .ToDictionary(p => p.Metadata.Name, p => Safe(p.OriginalValue));
                return ("Deleted", AuditData.Serialize(dict));
            }
            case EntityState.Modified:
            {
                var changes = new Dictionary<string, object?>();
                foreach (var p in entry.Properties)
                {
                    if (!p.IsModified) continue;
                    if (Equals(p.OriginalValue, p.CurrentValue)) continue;

                    if (AuditData.IsSensitive(p.Metadata.Name))
                    {
                        changes[p.Metadata.Name] = "***";
                        continue;
                    }
                    changes[p.Metadata.Name] = new { old = Safe(p.OriginalValue), @new = Safe(p.CurrentValue) };
                }
                if (changes.Count == 0) return (null, null);
                return ("Updated", AuditData.Serialize(changes));
            }
            default:
                return (null, null);
        }
    }

    private static string? GetPrimaryKey(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null) return null;
        var values = key.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? "");
        return string.Join(",", values);
    }

    private static (string?, string?, string?) ResolveSubject(object entity) => entity switch
    {
        ApplicationUser u => (u.IdentificationNumber, u.Name, u.PhoneNumber),
        GuestPatient g => (g.Id, g.Name, g.Phone),
        _ => (null, null, null)
    };

    private static object? Safe(object? value) => value switch
    {
        null => null,
        byte[] b => $"byte[{b.Length}]",
        _ => value
    };
}
