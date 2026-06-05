using Mediator;
using PiedraAzul.Application.Common.Models.Schedule;

namespace PiedraAzul.Application.Features.Doctors.Queries.GetScheduleConfig;

/// <summary>
/// Devuelve la configuración de agenda de un doctor: ventana de reserva, intervalo,
/// disponibilidad por día (derivada de sus slots activos) y los slots crudos.
/// </summary>
public record GetScheduleConfigByDoctorIdQuery(string DoctorId)
    : IRequest<ScheduleConfigDto>;
