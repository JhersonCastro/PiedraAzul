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

    // ── Solo para Type=REGISTERED: si además existe una cuenta invitada con la misma
    //    cédula y citas, se exponen aquí para ofrecer (con un 2º OTP al canal del invitado)
    //    el acceso a esas citas sin necesidad de iniciar sesión. ──

    /// <summary>Cantidad de citas bajo una cuenta invitada con la misma cédula (0 = no hay).</summary>
    public int GuestAppointmentCount { get; set; }

    /// <summary>Hash de verificación para el OTP del invitado (segundo OTP).</summary>
    public string? GuestVerificationHash { get; set; }

    public bool GuestHasPhone { get; set; }
    public bool GuestHasEmail { get; set; }
    public string? GuestMaskedPhone { get; set; }
    public string? GuestMaskedEmail { get; set; }
}

/// <summary>Paciente visto por el doctor (con fecha de última visita).</summary>
public class DoctorPatientType
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Identification { get; set; } = "";
    public string Phone { get; set; } = "";
    public PatientTypeEnum Type { get; set; }
    public DateTime? LastVisit { get; set; }
}

/// <summary>
/// Invitado con la misma cédula que el usuario registrado, candidato a vincular (merge).
/// No expone datos sin verificación: solo nombre, conteo de citas y canales enmascarados.
/// </summary>
public class MergeableGuestType
{
    /// <summary>Hash opaco para enviar/verificar OTP antes del merge.</summary>
    public string VerificationHash { get; set; } = "";

    /// <summary>Nombre con el que el invitado agendó (para que el usuario lo reconozca).</summary>
    public string GuestName { get; set; } = "";

    /// <summary>Cantidad de citas que se transferirían a la cuenta.</summary>
    public int AppointmentCount { get; set; }

    public bool HasPhone { get; set; }
    public bool HasEmail { get; set; }
    public string? MaskedPhone { get; set; }
    public string? MaskedEmail { get; set; }
}

/// <summary>Resultado de un merge guest → cuenta registrada.</summary>
public class MergeGuestResultType
{
    public bool Success { get; set; }
    public int MergedCount { get; set; }
    public string? Error { get; set; }
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
