namespace PiedraAzul.Contracts.Auth;

public class UserInfo
{
    public required string UserId { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string AvatarUrl { get; init; }
    public required List<string> Roles { get; init; }
}
