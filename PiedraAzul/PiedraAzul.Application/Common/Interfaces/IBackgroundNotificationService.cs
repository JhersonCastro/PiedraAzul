using PiedraAzul.Domain.Entities.Operations;

namespace PiedraAzul.Application.Common.Interfaces
{
    public interface IBackgroundNotificationService
    {
        /// <summary>
        /// Programa un recordatorio por email para cada delay indicado antes de la cita.
        /// Retorna los jobIds de Hangfire creados (para poder cancelarlos después).
        /// </summary>
        Task<List<string>> ScheduleAppointmentNotification(Guid appointmentId, DateTime appointmentStart, IEnumerable<TimeSpan> delays);

        /// <summary>
        /// Cancela los jobs de Hangfire con los ids indicados.
        /// </summary>
        Task CancelScheduleAppointmentNotification(params string[] jobIds);
    }
}
