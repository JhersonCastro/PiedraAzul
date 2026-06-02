using Mediator;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Notifications;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Application.Features.Appointments.Notifications;

/// <summary>
/// Envía un correo al paciente cuando su cita se crea, cancela o reagenda.
/// Resuelve el email del destinatario (usuario registrado o invitado). Si no hay email,
/// no hace nada. Nunca lanza: un fallo de correo no debe afectar la operación de la cita.
/// El diseño del correo vive en las plantillas de Infraestructura (EmailService/EmailTemplates).
/// </summary>
public sealed class AppointmentNotificationHandler : INotificationHandler<AppointmentNotification>
{
    private readonly IEmailService _email;
    private readonly IIdentityService _identity;
    private readonly IPatientGuestRepository _guests;
    private readonly IDoctorAvailabilitySlotRepository _slots;

    public AppointmentNotificationHandler(
        IEmailService email,
        IIdentityService identity,
        IPatientGuestRepository guests,
        IDoctorAvailabilitySlotRepository slots)
    {
        _email = email;
        _identity = identity;
        _guests = guests;
        _slots = slots;
    }

    public async ValueTask Handle(AppointmentNotification n, CancellationToken ct)
    {
        try
        {
            // 1. Resolver email y nombre del destinatario.
            string? toEmail = null;
            var patientName = "";

            if (!string.IsNullOrEmpty(n.PatientUserId))
            {
                var user = await _identity.GetById(n.PatientUserId);
                toEmail = user?.Email;
                patientName = user?.Name ?? "";
            }
            else if (!string.IsNullOrEmpty(n.PatientGuestId))
            {
                var guest = await _guests.GetByIdAsync(n.PatientGuestId, ct);
                toEmail = guest?.Email;
                patientName = guest?.Name ?? "";
            }

            // Sin email → no hay a quién notificar; se ignora.
            if (string.IsNullOrWhiteSpace(toEmail))
                return;

            // 2. Datos de la cita para el cuerpo del correo.
            var slot = await _slots.GetByIdAsync(n.SlotId, ct);
            var time = slot is not null ? TimeOnly.FromTimeSpan(slot.StartTime) : TimeOnly.MinValue;
            var start = n.Date.ToDateTime(time);

            var doctor = await _identity.GetById(n.DoctorId);
            var doctorName = doctor?.Name ?? "tu especialista";

            // 3. Enviar usando la plantilla correspondiente. Los métodos ya capturan sus errores.
            var task = n.Change switch
            {
                AppointmentChange.Created => _email.SendAppointmentCreatedAsync(toEmail!, patientName, doctorName, start),
                AppointmentChange.Rescheduled => _email.SendAppointmentRescheduledAsync(toEmail!, patientName, doctorName, start),
                AppointmentChange.Cancelled => _email.SendAppointmentCancelledAsync(toEmail!, patientName, doctorName, start),
                _ => Task.FromResult(false)
            };
            await task;
        }
        catch
        {
            // Defensivo: jamás propagar un fallo de notificación a la operación de la cita.
        }
    }
}
