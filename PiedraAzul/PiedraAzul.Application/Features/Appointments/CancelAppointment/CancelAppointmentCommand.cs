using Mediator;

namespace PiedraAzul.Application.Features.Appointments.CancelAppointment;

/// <summary>Cancela una cita existente. El paciente solo puede cancelar sus propias citas activas.</summary>
public record CancelAppointmentCommand(
    Guid AppointmentId,
    string RequestingUserId
) : IRequest<bool>;
