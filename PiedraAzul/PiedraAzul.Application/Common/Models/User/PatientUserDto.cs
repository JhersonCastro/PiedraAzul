namespace PiedraAzul.Application.Common.Models.User;

/// <summary>
/// Datos de un usuario registrado necesarios para listarlo como paciente de un doctor.
/// Solo se devuelven usuarios no eliminados.
/// </summary>
public record PatientUserDto(
    string Id,
    string Name,
    string Identification,
    string Phone);
