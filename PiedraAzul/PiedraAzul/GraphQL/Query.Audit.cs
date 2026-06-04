using HotChocolate;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using PiedraAzul.GraphQL.Inputs;
using PiedraAzul.GraphQL.Types;
using PiedraAzul.Infrastructure.Persistence;

namespace PiedraAzul.GraphQL;

public partial class Query
{
    /// <summary>Listado de auditoría con filtros, búsqueda rápida y paginación (solo Admin).</summary>
    [Authorize(Roles = new[] { "Admin" })]
    public async Task<AuditLogListType> GetAuditLogsAsync(
        AuditFilterInput filter,
        [Service] AppDbContext db)
    {
        var q = db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
            q = q.Where(a => a.EntityType == filter.EntityType);

        if (!string.IsNullOrWhiteSpace(filter.Action))
            q = q.Where(a => a.Action == filter.Action);

        if (!string.IsNullOrWhiteSpace(filter.Source))
            q = q.Where(a => a.Source == filter.Source);

        if (!string.IsNullOrWhiteSpace(filter.ActorUserId))
            q = q.Where(a => a.ActorUserId == filter.ActorUserId);

        if (filter.DateFrom.HasValue)
            q = q.Where(a => a.Timestamp >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
            q = q.Where(a => a.Timestamp <= filter.DateTo.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var s = $"%{filter.SearchText.Trim()}%";
            q = q.Where(a =>
                EF.Functions.ILike(a.SubjectIdentification ?? "", s) ||
                EF.Functions.ILike(a.SubjectName ?? "", s) ||
                EF.Functions.ILike(a.SubjectPhone ?? "", s) ||
                EF.Functions.ILike(a.ActorName ?? "", s) ||
                EF.Functions.ILike(a.EntityId ?? "", s) ||
                EF.Functions.ILike(a.EntityType, s) ||
                EF.Functions.ILike(a.Action, s));
        }

        var total = await q.CountAsync();
        var page = Math.Max(1, filter.PageNumber);
        var size = Math.Clamp(filter.PageSize, 1, 100);

        var items = await q
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(a => new AuditLogType
            {
                Id = a.Id,
                Timestamp = a.Timestamp,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Action = a.Action,
                Source = a.Source,
                ActorUserId = a.ActorUserId,
                ActorName = a.ActorName,
                ActorRoles = a.ActorRoles,
                IpAddress = a.IpAddress,
                SubjectIdentification = a.SubjectIdentification,
                SubjectName = a.SubjectName,
                SubjectPhone = a.SubjectPhone,
                Data = a.Data
            })
            .ToListAsync();

        return new AuditLogListType
        {
            Items = items,
            TotalCount = total,
            PageNumber = page,
            PageSize = size,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }

    /// <summary>Valores distintos de entidad y acción para los filtros (solo Admin).</summary>
    [Authorize(Roles = new[] { "Admin" })]
    public async Task<AuditFilterOptionsType> GetAuditFilterOptionsAsync([Service] AppDbContext db)
    {
        var entityTypes = await db.AuditLogs.AsNoTracking()
            .Select(a => a.EntityType).Distinct().OrderBy(x => x).ToListAsync();
        var actions = await db.AuditLogs.AsNoTracking()
            .Select(a => a.Action).Distinct().OrderBy(x => x).ToListAsync();

        return new AuditFilterOptionsType { EntityTypes = entityTypes, Actions = actions };
    }
}
