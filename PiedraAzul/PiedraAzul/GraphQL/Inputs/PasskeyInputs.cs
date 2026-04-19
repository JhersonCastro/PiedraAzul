namespace PiedraAzul.GraphQL.Inputs;

public record BeginPasskeyRegistrationInput(string UserId, string Email, string DisplayName);

public record CompletePasskeyRegistrationInput(
    string UserId,
    string AttestationResponse,
    string FriendlyName);

public record CompletePasskeyAssertionInput(string AssertionResponse);
