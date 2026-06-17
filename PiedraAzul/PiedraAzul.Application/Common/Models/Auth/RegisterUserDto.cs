namespace PiedraAzul.Application.Common.Models.Auth
{
    public record RegisterUserDto(
        string Email,
        string Name,
        string? PhoneNumber,
        string? IdentificationNumber
    );
}