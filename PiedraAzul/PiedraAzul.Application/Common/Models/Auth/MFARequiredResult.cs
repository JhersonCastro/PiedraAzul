namespace PiedraAzul.Application.Common.Models.Auth;

public record MFARequiredResult(
    string MFAToken,
    string MFAMethod,
    bool HasEmail
);
