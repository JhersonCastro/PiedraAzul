using Mediator;
using PiedraAzul.Domain.Entities.Operations;

namespace PiedraAzul.Application.Features.Appointments.RescheduleAppointment;

/// <summary>
/// Reagenda una cita existente. El permiso se deriva de los roles del usuario solicitante
/// (paciente dueño, doctor de la cita o administrador), no de un flag externo.
/// </summary>
public record RescheduleAppointmentCommand(
    Guid AppointmentId,
    string RequestingUserId,
    Guid NewSlotId,
    DateOnly NewDate
) : IRequest<Appointment>;
