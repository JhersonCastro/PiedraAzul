namespace PiedraAzul.GraphQL.Inputs;

public class AuditFilterInput
{
    /// <summary>Búsqueda rápida: cédula, nombre, teléfono, actor, entidad o acción.</summary>
    public string? SearchText { get; set; }
    public string? EntityType { get; set; }
    public string? Action { get; set; }
    /// <summary>"Interceptor" | "Business" | null para todos.</summary>
    public string? Source { get; set; }
    public string? ActorUserId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
