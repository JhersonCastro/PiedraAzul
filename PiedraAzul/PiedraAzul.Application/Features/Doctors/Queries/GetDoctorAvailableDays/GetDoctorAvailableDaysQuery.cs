using Mediator;

namespace PiedraAzul.Application.Features.Doctors.Queries.GetDoctorAvailableDays;

/// <summary>
/// Retorna los días que tienen al menos un slot disponible para el doctor
/// dentro de un rango de fechas. Realiza solo 2 consultas a la BD en lugar de
/// una consulta por cada día (evita el problema de DbContext concurrente).
/// </summary>
public sealed record GetDoctorAvailableDaysQuery(
    string DoctorId,
    DateOnly StartDate,
    int NumberOfDays
) : IRequest<IReadOnlyList<DateOnly>>;
