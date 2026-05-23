using Mediator;
using PiedraAzul.Domain.Entities.Operations;

namespace PiedraAzul.Application.Features.Appointments.RescheduleAppointment;

/// <summary>Reagenda una cita para un guest verificado mediante hash OTP.</summary>
public record RescheduleAppointmentByHashCommand(
    string VerificationHash,
    Guid AppointmentId,
    Guid NewSlotId,
    DateOnly NewDate
) : IRequest<Appointment>;
