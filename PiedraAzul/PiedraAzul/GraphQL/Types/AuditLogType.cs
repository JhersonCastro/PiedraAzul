namespace PiedraAzul.GraphQL.Types;

public class AuditLogType
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string EntityType { get; set; } = "";
    public string? EntityId { get; set; }
    public string Action { get; set; } = "";
    public string Source { get; set; } = "";
    public string? ActorUserId { get; set; }
    public string? ActorName { get; set; }
    public string? ActorRoles { get; set; }
    public string? IpAddress { get; set; }
    public string? SubjectIdentification { get; set; }
    public string? SubjectName { get; set; }
    public string? SubjectPhone { get; set; }
    public string? Data { get; set; }
}

public class AuditLogListType
{
    public List<AuditLogType> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>Valores distintos para poblar los dropdowns de filtro.</summary>
public class AuditFilterOptionsType
{
    public List<string> EntityTypes { get; set; } = new();
    public List<string> Actions { get; set; } = new();
}
