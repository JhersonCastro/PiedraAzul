namespace PiedraAzul.Contracts.DTOs;

/// <summary>
/// Invitado con la misma cédula que el usuario registrado, candidato a vincular (merge).
/// No expone datos sin verificación: solo nombre, conteo de citas y canales enmascarados.
/// Tipo compartido: el servidor lo expone vía GraphQL y el cliente lo deserializa.
/// </summary>
public class MergeableGuestDto
{
    public string VerificationHash { get; set; } = "";
    public string GuestName { get; set; } = "";
    public int AppointmentCount { get; set; }
    public bool HasPhone { get; set; }
    public bool HasEmail { get; set; }
    public string? MaskedPhone { get; set; }
    public string? MaskedEmail { get; set; }
}
