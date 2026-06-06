using Hangfire;
using Mediator;
using Microsoft.Extensions.Logging;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Notifications;
using PiedraAzul.Domain.Entities.Operations;

namespace PiedraAzul.Infrastructure.BackgroundJobs
{
    public class BackgroundNotificationService(
        IBackgroundJobClient jobClient,
        IMediator mediator,
        ILogger<BackgroundNotificationService> logger) : IBackgroundNotificationService
    {
        // Colombia no usa horario de verano: siempre UTC-5.
        private static readonly TimeZoneInfo ColombiaZone = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "SA Pacific Standard Time" : "America/Bogota");

        public Task CancelScheduleAppointmentNotification(params string[] jobIds)
        {
            foreach (var jobId in jobIds)
            {
                bool deleted = jobClient.Delete(jobId);
                if (!deleted)
                    logger.LogWarning("No se pudo cancelar el job {JobId} (ya ejecutado o no existe).", jobId);
            }
            return Task.CompletedTask;
        }

        public async Task<List<string>> ScheduleAppointmentNotification(Guid appointmentId, DateTime appointmentStart, IEnumerable<TimeSpan> delays)
        {
            var jobsId = new List<string>();

            // appointmentStart viene en hora colombiana (sin timezone).
            // Convertimos a UTC para que Hangfire programe correctamente.
            var appointmentStartUtc = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(appointmentStart, DateTimeKind.Unspecified),
                ColombiaZone);

            foreach (var delay in delays)
            {
                var reminderTime = new DateTimeOffset(appointmentStartUtc, TimeSpan.Zero) - delay;

                // Solo programar si el recordatorio es en el futuro
                if (reminderTime <= DateTimeOffset.UtcNow)
                {
                    logger.LogWarning("Recordatorio para cita {AppointmentId} con delay {Delay} ya pasó, se omite.", appointmentId, delay);
                    continue;
                }

                string jobId = jobClient.Schedule(
                    () => SendAppointmentNotification(appointmentId, appointmentStart),
                    reminderTime);

                jobsId.Add(jobId);
            }

            return jobsId;
        }

        // [AutomaticRetry] hace que Hangfire reintente hasta 3 veces si el email falla.
        [AutomaticRetry(Attempts = 3)]
        public async Task SendAppointmentNotification(Guid appointmentId, DateTime appointmentStart)
        {
            await mediator.Publish(new AppointmentScheduleNotification(appointmentId, appointmentStart));
        }
    }
}
