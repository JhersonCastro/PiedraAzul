using Mediator;
using PiedraAzul.Domain.Entities.Operations;

namespace PiedraAzul.Application.Features.Appointments.RescheduleAppointment;

/// <summary>Reagenda una cita existente para un paciente autenticado.</summary>
public record RescheduleAppointmentCommand(
    Guid AppointmentId,
    string RequestingUserId,
    Guid NewSlotId,
    DateOnly NewDate
) : IRequest<Appointment>;
