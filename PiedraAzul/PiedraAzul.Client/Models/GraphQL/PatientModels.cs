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

    // Solo para Type=REGISTERED: cuenta invitada con la misma cédula y citas (2º OTP).
    public int GuestAppointmentCount { get; set; }
    public string? GuestVerificationHash { get; set; }
    public bool GuestHasPhone { get; set; }
    public bool GuestHasEmail { get; set; }
    public string? GuestMaskedPhone { get; set; }
    public string? GuestMaskedEmail { get; set; }
}

/// <summary>Paciente visto por un doctor (con fecha de última visita).</summary>
public class DoctorPatientGQL
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Identification { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Type { get; set; } = "";
    public DateTime? LastVisit { get; set; }
}

