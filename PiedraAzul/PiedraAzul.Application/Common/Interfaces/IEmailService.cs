namespace PiedraAzul.Application.Common.Interfaces;

public interface IEmailService
{
    Task<bool> SendPasswordResetEmailAsync(string email, string userName, string resetLink);
    Task<bool> SendMFAEmailAsync(string email, string userName, string otp, int expirationMinutes);
    Task<bool> SendAccountLockedEmailAsync(string email, string userName);
    Task<bool> SendMFASetupConfirmationAsync(string email, string userName, string mfaMethod);
    Task<bool> SendGenericEmailAsync(string to, string subject, string htmlBody);
}
