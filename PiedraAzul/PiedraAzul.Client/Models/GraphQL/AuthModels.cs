namespace PiedraAzul.Client.Models.GraphQL;

public class UserGQL
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string AvatarUrl { get; set; } = "default.png";
    public List<string> Roles { get; set; } = new();
}

public class PasskeyGQL
{
    public string Id { get; set; } = "";
    public string FriendlyName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
