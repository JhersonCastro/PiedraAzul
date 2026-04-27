namespace PiedraAzul.Application.Common.Interfaces;

public interface IMFAService
{
    Task<bool> IsEnabledAsync(string userId);
    Task<string> GetMFAMethodAsync(string userId);
    Task<bool> EnableMFAAsync(string userId, string method);
    Task<bool> DisableMFAAsync(string userId);
    Task<bool> VerifyOTPAsync(string userId, string otp);
    Task<string> GenerateOTPAsync(string userId);
}
