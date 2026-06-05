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
/// Lo consumen varios handlers como consecuencia del cambio de estado:
///  - notificación por email al paciente (registrado o invitado),
///  - invalidación del cache de lectura del doctor.
/// <para>
/// <see cref="DoctorId"/>/<see cref="Date"/> son el día afectado por el cambio (en un
/// reagendamiento, el nuevo). <see cref="OldDoctorId"/>/<see cref="OldDate"/> solo se llenan
/// en un reagendamiento para refrescar también el día/doctor de origen.
/// </para>
/// </summary>
public sealed record AppointmentNotification(
    AppointmentChange Change,
    string? PatientUserId,
    string? PatientGuestId,
    string DoctorId,
    Guid SlotId,
    DateOnly Date,
    string? OldDoctorId = null,
    DateOnly? OldDate = null
) : INotification;
