namespace PiedraAzul.Client.Models.GraphQL;

public class PatientSearchResultGQL
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Identification { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Type { get; set; } = "";
}

/// <summary>
/// Resultado de lookup por identificación.
/// Type puede ser: "REGISTERED" | "GUEST" | "NO_CONTACT" (registrado sin canales)
/// </summary>
public class GuestLookupResultGQL
{
    public string VerificationHash { get; set; } = "";
    public bool HasPhone { get; set; }
    public bool HasEmail { get; set; }
    public string? MaskedPhone { get; set; }
    public string? MaskedEmail { get; set; }
    public string Type { get; set; } = "";
}

/// <summary>Datos del usuario tras verificación de OTP.</summary>
public class GuestDataGQL
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string SessionType { get; set; } = "";
}
