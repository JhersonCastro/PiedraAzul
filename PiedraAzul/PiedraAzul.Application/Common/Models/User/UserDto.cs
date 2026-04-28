namespace PiedraAzul.Application.Common.Models.User
{
    public record UserDto(
        string Id,
        string Email,
        string Name,
        string AvatarUrl,
        bool EmailConfirmed = false
    );
}