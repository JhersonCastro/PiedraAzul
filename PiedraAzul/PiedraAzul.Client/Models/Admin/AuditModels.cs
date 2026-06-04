namespace PiedraAzul.Client.Models.Admin;

public class AuditLogGQL
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

public class AuditLogListGQL
{
    public List<AuditLogGQL> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class AuditFilterOptionsGQL
{
    public List<string> EntityTypes { get; set; } = new();
    public List<string> Actions { get; set; } = new();
}

public class AuditFilterModel
{
    public string? SearchText { get; set; }
    public string? EntityType { get; set; }
    public string? Action { get; set; }
    public string? Source { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
