namespace PiedraAzul.Application.Common.Interfaces;

public record PasskeyDto(Guid Id, string FriendlyName, DateTime CreatedAt);

public interface IPasskeyService
{
    Task<string> BeginRegistrationAsync(string userId, string email, string displayName);
    Task<bool> CompleteRegistrationAsync(string userId, string attestationResponseJson, string friendlyName);
    Task<string> BeginAssertionAsync();
    Task<(string UserId, List<string> Roles)> CompleteAssertionAsync(string assertionResponseJson);
    Task<List<PasskeyDto>> GetUserPasskeysAsync(string userId);
    Task<bool> DeletePasskeyAsync(string userId, Guid passkeyId);
}
