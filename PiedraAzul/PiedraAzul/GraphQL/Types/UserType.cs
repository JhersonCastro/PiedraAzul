namespace PiedraAzul.GraphQL.Types;

public class UserType
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public List<string> Roles { get; set; } = new();
}

public class AuthResponseType
{
    public string AccessToken { get; set; } = "";
    public UserType User { get; set; } = new();
}
