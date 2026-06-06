namespace PiedraAzul.Application.Common.Interfaces;

public interface IEmailService
{
    Task<bool> SendPasswordResetEmailAsync(string email, string userName, string resetLink);
    Task<bool> SendMFAEmailAsync(string email, string userName, string otp, int expirationMinutes);
    Task<bool> SendAccountLockedEmailAsync(string email, string userName);
    Task<bool> SendMFASetupConfirmationAsync(string email, string userName, string mfaMethod);
    Task<bool> SendGenericEmailAsync(string to, string subject, string htmlBody);
    Task<bool> SendWelcomeWithPasswordAsync(string email, string userName, string tempPassword, string role);

    Task<bool> SendAppointmentCreatedAsync(string email, string patientName, string doctorName, DateTime start);
    Task<bool> SendAppointmentRescheduledAsync(string email, string patientName, string doctorName, DateTime start);
    Task<bool> SendAppointmentCancelledAsync(string email, string patientName, string doctorName, DateTime start);
    Task<bool> SendReminderAppointment(string email, DateTime appointmentStart, string patientName, string doctorName);
}
