using Microsoft.JSInterop;
using PiedraAzul.Client.Models;
using PiedraAzul.Client.Models.GraphQL;
using PiedraAzul.Client.Services.GraphQLServices;
using PiedraAzul.Client.Services.Wrappers;

namespace PiedraAzul.Client.Services.AuthServices;

public class PasskeyService(GraphQLHttpClient graphQL, IJSRuntime js)
{
    public async Task<bool> IsSupportedAsync()
    {
        try
        {
            return await js.InvokeAsync<bool>("passkeyInterop.isSupported");
        }
        catch
        {
            return false;
        }
    }

    public async Task<Result<bool>> RegisterAsync(string userId, string email, string displayName, string friendlyName)
    {
        return await GraphQLExecutor.Execute(async () =>
        {
            const string beginMutation = """
                mutation BeginPasskeyRegistration($input: BeginPasskeyRegistrationInput!) {
                    beginPasskeyRegistration(input: $input)
                }
                """;

            var optionsJson = await graphQL.ExecuteAsync<string>(
                beginMutation,
                new { input = new { userId, email, displayName } },
                "beginPasskeyRegistration");

            var attestationResponse = await js.InvokeAsync<string>("passkeyInterop.register", optionsJson);

            const string completeMutation = """
                mutation CompletePasskeyRegistration($input: CompletePasskeyRegistrationInput!) {
                    completePasskeyRegistration(input: $input)
                }
                """;

            return await graphQL.ExecuteAsync<bool>(
                completeMutation,
                new { input = new { userId, attestationResponse, friendlyName } },
                "completePasskeyRegistration");
        });
    }

    public async Task<Result<UserGQL>> LoginAsync()
    {
        return await GraphQLExecutor.Execute(async () =>
        {
            const string beginMutation = """
                mutation BeginPasskeyAssertion {
                    beginPasskeyAssertion
                }
                """;

            var optionsJson = await graphQL.ExecuteAsync<string>(beginMutation, null, "beginPasskeyAssertion");

            var assertionResponse = await js.InvokeAsync<string>("passkeyInterop.authenticate", optionsJson);

            const string completeMutation = """
                mutation CompletePasskeyAssertion($input: CompletePasskeyAssertionInput!) {
                    completePasskeyAssertion(input: $input) {
                        id name email roles avatarUrl
                    }
                }
                """;

            var user = await graphQL.ExecuteAsync<UserGQL>(
                completeMutation,
                new { input = new { assertionResponse } },
                "completePasskeyAssertion");

            return user!;
        });
    }

    public async Task<Result<List<PasskeyGQL>>> GetMyPasskeysAsync()
    {
        const string query = """
            query MyPasskeys {
                myPasskeys { id friendlyName createdAt }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var list = await graphQL.ExecuteAsync<List<PasskeyGQL>>(query, null, "myPasskeys");
            return list ?? [];
        });
    }

    public async Task<Result<bool>> DeletePasskeyAsync(string passkeyId)
    {
        const string mutation = """
            mutation DeletePasskey($passkeyId: String!) {
                deletePasskey(passkeyId: $passkeyId)
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
            await graphQL.ExecuteAsync<bool>(mutation, new { passkeyId }, "deletePasskey"));
    }
}
