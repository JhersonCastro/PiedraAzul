using Mediator;

namespace PiedraAzul.Application.Common.Notifications;

public enum AppointmentChange
{
    Created,
    Cancelled,
    Rescheduled
}

/// <summary>
/// Evento de dominio de aplicación: una cita fue creada, cancelada o reagendada.
/// Un handler resuelve el email del paciente (registrado o invitado) y le notifica.
/// Si el paciente no tiene email, se ignora silenciosamente.
/// </summary>
public sealed record AppointmentNotification(
    AppointmentChange Change,
    string? PatientUserId,
    string? PatientGuestId,
    string DoctorId,
    Guid SlotId,
    DateOnly Date
) : INotification;
