namespace PiedraAzul.Client.Models.GraphQL;

public class UserGQL
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public List<string> Roles { get; set; } = new();
}

public class AuthResponseGQL
{
    public string AccessToken { get; set; } = "";
    public UserGQL User { get; set; } = new();
}
