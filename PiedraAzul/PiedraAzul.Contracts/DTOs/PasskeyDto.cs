namespace PiedraAzul.Contracts.DTOs;

/// <summary>
/// Passkey (credencial WebAuthn) registrada por un usuario.
/// Tipo compartido: el servidor lo expone vía GraphQL y el cliente lo deserializa.
/// </summary>
public class PasskeyDto
{
    public string Id { get; set; } = "";
    public string FriendlyName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
