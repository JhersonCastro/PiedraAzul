namespace PiedraAzul.Application.Common.Interfaces;

public interface IMFATokenService
{
    string GenerateMFAToken(string userId);
    string? ValidateMFAToken(string token);
}
