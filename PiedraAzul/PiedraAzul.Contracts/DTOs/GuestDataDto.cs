namespace PiedraAzul.Contracts.DTOs;

/// <summary>
/// Datos del usuario/invitado devueltos tras verificar OTP por hash.
/// Tipo compartido: el servidor lo expone vía GraphQL y el cliente lo deserializa.
/// </summary>
public class GuestDataDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string SessionType { get; set; } = "";
}
