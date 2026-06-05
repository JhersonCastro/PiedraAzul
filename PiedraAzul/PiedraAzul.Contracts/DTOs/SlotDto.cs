namespace PiedraAzul.Contracts.DTOs;

/// <summary>
/// Slot de disponibilidad expuesto por GraphQL y consumido por el cliente.
/// Tipo compartido (single source of truth): el servidor lo devuelve como tipo GraphQL
/// y el cliente deserializa la respuesta directamente en él.
/// </summary>
public class SlotDto
{
    public string Id { get; set; } = "";
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool IsAvailable { get; set; }
}
