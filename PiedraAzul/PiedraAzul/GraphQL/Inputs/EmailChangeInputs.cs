namespace PiedraAzul.GraphQL.Inputs;

public record RequestEmailChangeInput(string NewEmail);

public record ConfirmEmailChangeInput(string NewEmail, string Code);
