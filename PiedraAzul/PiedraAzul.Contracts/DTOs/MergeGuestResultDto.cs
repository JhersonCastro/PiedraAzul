namespace PiedraAzul.Contracts.DTOs;

/// <summary>
/// Resultado de un merge guest → cuenta registrada.
/// Tipo compartido: el servidor lo expone vía GraphQL y el cliente lo deserializa.
/// </summary>
public class MergeGuestResultDto
{
    public bool Success { get; set; }
    public int MergedCount { get; set; }
    public string? Error { get; set; }
}
