namespace PiedraAzul.GraphQL.Inputs;

public record RegisterInput(
    string Email,
    string Password,
    string Name,
    string Phone,
    string IdentificationNumber,
    List<string> Roles
);
