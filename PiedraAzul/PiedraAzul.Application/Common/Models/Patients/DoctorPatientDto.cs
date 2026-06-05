namespace PiedraAzul.Application.Common.Models.Patients;

/// <summary>Tipo de paciente visto desde la agenda del doctor.</summary>
public enum PatientKind { Unknown, Registered, Guest, NoContact }

/// <summary>
/// Paciente (registrado o invitado) que tiene o tuvo citas con un doctor,
/// con la fecha de su última visita.
/// </summary>
public class DoctorPatientDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Identification { get; set; } = "";
    public string Phone { get; set; } = "";
    public PatientKind Type { get; set; }
    public DateTime? LastVisit { get; set; }
}
