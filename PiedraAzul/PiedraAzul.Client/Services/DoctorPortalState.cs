using PiedraAzul.Client.Models.UserProfiles;

namespace PiedraAzul.Client.Services;

/// <summary>
/// Estado compartido del portal del doctor para pasar datos entre tabs
/// sin salir de /account.
/// </summary>
public class DoctorPortalState
{
    /// <summary>Fecha seleccionada para el cronograma diario.</summary>
    public DateTime SelectedDate { get; set; } = DateTime.Today;

    /// <summary>
    /// Paciente pre-seleccionado para el agendamiento manual.
    /// Se setea al hacer clic en "+ Cita nueva" desde Mis Pacientes.
    /// </summary>
    public PatientModel? PreselectedPatient { get; set; }
}
