using Mediator;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Notifications;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Application.Features.Appointments.Notifications
{
    internal class AppointmentScheduleHandler(
        IAppointmentRepository appointmentRepository,
        IIdentityService identityService,
        IPatientGuestRepository patientGuestRepository,
        IEmailService emailService) : INotificationHandler<AppointmentScheduleNotification>
    {
        public async ValueTask Handle(AppointmentScheduleNotification notification, CancellationToken cancellationToken)
        {
            var appointment = await appointmentRepository.GetByIdAsync(notification.AppointmentId, cancellationToken);

            // Cita eliminada o ya no existe
            if (appointment is null) return;

            // Si la cita ya fue cancelada o reagendada, no enviar recordatorio.
            if (appointment.Status != Domain.Entities.Operations.AppointmentStatus.Active) return;

            var doctor = await identityService.GetById(appointment.DoctorId);
            if (doctor is null) return;

            if (!string.IsNullOrEmpty(appointment.PatientUserId))
            {
                var patient = await identityService.GetById(appointment.PatientUserId);
                if (patient is null) return;
                if (!patient.EmailConfirmed) return;

                await emailService.SendReminderAppointment(
                    patient.Email,
                    notification.AppointmentStart,
                    patient.Name,
                    doctor.Name);
            }
            else if (!string.IsNullOrEmpty(appointment.PatientGuestId))
            {
                var patient = await patientGuestRepository.GetByIdAsync(appointment.PatientGuestId, cancellationToken);
                if (patient is null) return;
                if (string.IsNullOrEmpty(patient.Email)) return;

                await emailService.SendReminderAppointment(
                    patient.Email,
                    notification.AppointmentStart,
                    patient.Name,
                    doctor.Name);
            }
        }
    }
}
