namespace PiedraAzul.Application.Common.Interfaces;

public record MFAStatus(bool EmailOTPEnabled, bool TOTPEnabled, bool HasBackupCodes);

public interface IMFAService
{
    Task<MFAStatus> GetMFAStatusAsync(string userId);
    Task<bool> IsEnabledAsync(string userId);
    Task<string> GetMFAMethodAsync(string userId);
    Task<List<string>> EnableMFAAsync(string userId, string method);
    Task<bool> DisableMFAAsync(string userId, string method = "");
    Task<bool> VerifyOTPAsync(string userId, string otp);
    Task<string> GenerateOTPAsync(string userId);

    // TOTP Methods
    Task<string> GenerateTOTPSecretAsync(string userId);
    Task<string> GetTOTPQRCodeAsync(string userId, string email);
    Task<bool> VerifyTOTPAsync(string userId, string totp);
    Task<bool> ConfirmTOTPSetupAsync(string userId, string totp);

    // Backup Codes
    Task<List<string>> GenerateBackupCodesAsync(string userId);
    Task<bool> VerifyBackupCodeAsync(string userId, string code);

    // Email sending
    Task<bool> SendOTPEmailAsync(string userId, string email);
}
