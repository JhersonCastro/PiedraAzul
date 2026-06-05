using Mediator;
using PiedraAzul.Application.Common.Models.Patients;

namespace PiedraAzul.Application.Features.Doctors.Queries.GetDoctorPatients;

/// <summary>
/// Devuelve los pacientes únicos (registrados e invitados) que tienen o tuvieron
/// citas con el doctor indicado, deduplicando por identificación.
/// </summary>
public record GetDoctorPatientsQuery(string DoctorId)
    : IRequest<IReadOnlyList<DoctorPatientDto>>;
