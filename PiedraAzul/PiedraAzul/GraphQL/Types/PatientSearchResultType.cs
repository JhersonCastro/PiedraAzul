namespace PiedraAzul.GraphQL.Types;

public enum PatientTypeEnum { Unknown, Registered, Guest, NoContact }

public class PatientSearchResultType
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Identification { get; set; } = "";
    public string Phone { get; set; } = "";
    public PatientTypeEnum Type { get; set; }
}

/// <summary>
/// Resultado del lookup de identificación. No expone datos personales sin verificación.
/// Para REGISTERED y GUEST se devuelve un hash de verificación y los canales disponibles (enmascarados).
/// </summary>
public class GuestLookupResultType
{
    /// <summary>Hash opaco para continuar el flujo de verificación con OTP.</summary>
    public string VerificationHash { get; set; } = "";

    /// <summary>El usuario tiene teléfono registrado.</summary>
    public bool HasPhone { get; set; }

    /// <summary>El usuario tiene email registrado.</summary>
    public bool HasEmail { get; set; }

    /// <summary>Teléfono enmascarado, ej: "Terminado en: 4444".</summary>
    public string? MaskedPhone { get; set; }

    /// <summary>Email enmascarado, ej: "j***o@gmail.com".</summary>
    public string? MaskedEmail { get; set; }

    /// <summary>"GUEST" | "REGISTERED" | "NoContact" (registrado pero sin canales)</summary>
    public PatientTypeEnum Type { get; set; }
}

/// <summary>Datos del usuario devueltos tras verificación exitosa por hash.</summary>
public class GuestDataType
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";

    /// <summary>"Guest" o "RegisteredUser" para que el cliente sepa el tipo.</summary>
    public string SessionType { get; set; } = "";
}
