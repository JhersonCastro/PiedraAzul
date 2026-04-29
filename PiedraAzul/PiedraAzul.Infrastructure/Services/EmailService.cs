using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Infrastructure.Email;
using System.Net;
using System.Net.Mail;

namespace PiedraAzul.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly SmtpClient _smtpClient;
    private readonly string _fromAddress;
    private readonly string _fromName;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var smtpConfig = _configuration.GetSection("Email:Smtp");
        var host = smtpConfig["Host"] ?? "smtp.gmail.com";
        var port = int.Parse(smtpConfig["Port"] ?? "587");
        var username = smtpConfig["Username"] ?? "";
        var password = smtpConfig["Password"] ?? "";
        _fromAddress = smtpConfig["FromAddress"] ?? "noreply@piedraazul.com";
        _fromName = smtpConfig["FromName"] ?? "Piedra Azul";

        _smtpClient = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true,
            Timeout = 10000
        };
    }

    public async Task<bool> SendPasswordResetEmailAsync(string email, string userName, string resetLink)
    {
        try
        {
            var subject = "Restablecer tu contraseña - Piedra Azul";
            var htmlBody = EmailTemplates.PasswordResetTemplate(userName, resetLink);
            return await SendGenericEmailAsync(email, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset email to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendMFAEmailAsync(string email, string userName, string otp, int expirationMinutes)
    {
        try
        {
            var subject = "Tu código de verificación - Piedra Azul";
            var htmlBody = EmailTemplates.MFAVerificationTemplate(userName, otp, expirationMinutes);
            return await SendGenericEmailAsync(email, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending MFA email to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendAccountLockedEmailAsync(string email, string userName)
    {
        try
        {
            var subject = "Tu cuenta ha sido bloqueada - Piedra Azul";
            var htmlBody = EmailTemplates.AccountLockedTemplate(userName);
            return await SendGenericEmailAsync(email, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending account locked email to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendMFASetupConfirmationAsync(string email, string userName, string mfaMethod)
    {
        try
        {
            var subject = "Verificación de dos factores activada - Piedra Azul";
            var htmlBody = EmailTemplates.MFASetupConfirmationTemplate(userName, mfaMethod);
            return await SendGenericEmailAsync(email, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending MFA setup confirmation email to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendGenericEmailAsync(string to, string subject, string htmlBody)
    {
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_fromAddress, _fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(to);

            await _smtpClient.SendMailAsync(message);

            _logger.LogInformation("Email sent successfully to {To}", to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {To}", to);
            return false;
        }
    }
}
