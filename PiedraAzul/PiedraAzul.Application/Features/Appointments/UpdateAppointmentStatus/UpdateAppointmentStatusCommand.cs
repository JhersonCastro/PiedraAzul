using Mediator;
using PiedraAzul.Domain.Entities.Operations;

namespace PiedraAzul.Application.Features.Appointments.UpdateAppointmentStatus;

/// <summary>
/// Permite al doctor marcar una cita como Completada o Inasistencia.
/// Solo puede actuar sobre sus propias citas y solo si el horario de la cita ya pasó (hora Colombia).
/// </summary>
public record UpdateAppointmentStatusCommand(
    Guid AppointmentId,
    string DoctorUserId,
    AppointmentStatus NewStatus) : IRequest<bool>;
