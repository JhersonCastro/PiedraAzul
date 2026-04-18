using Microsoft.JSInterop;
using PiedraAzul.Client.Models;
using PiedraAzul.Client.Models.GraphQL;
using PiedraAzul.Client.Models.UserProfiles;
using PiedraAzul.Client.Services.GraphQLServices;
using PiedraAzul.Client.Services.Wrappers;

namespace PiedraAzul.Client.Services.AuthServices;

public class AuthenticationService
{
    private readonly GraphQLHttpClient graphQL;
    private readonly IJSRuntime js;

    public AuthenticationService(GraphQLHttpClient graphQL, IJSRuntime js)
    {
        this.graphQL = graphQL;
        this.js = js;
    }

    public async Task<Result<UserGQL>> RegisterAsync(RegisterModel registerModel, string role)
    {
        const string mutation = """
            mutation Register($input: RegisterInput!) {
                register(input: $input) {
                    accessToken
                    user { id name email roles }
                }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var response = await graphQL.ExecuteAsync<AuthResponseGQL>(
                mutation,
                new
                {
                    input = new
                    {
                        email = registerModel.Email ?? "",
                        password = registerModel.Password ?? "",
                        name = registerModel.FullName ?? "",
                        phone = registerModel.Phone ?? "",
                        identificationNumber = registerModel.Document ?? "",
                        roles = new[] { role }
                    }
                },
                "register");

            await js.InvokeVoidAsync("sessionStorage.setItem", "accessToken", response!.AccessToken);
            return response.User;
        });
    }

    public async Task<Result<UserGQL>> LoginAsync(LoginModel loginModel)
    {
        const string mutation = """
            mutation Login($input: LoginInput!) {
                login(input: $input) {
                    accessToken
                    user { id name email roles }
                }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var response = await graphQL.ExecuteAsync<AuthResponseGQL>(
                mutation,
                new { input = new { email = loginModel.Login, password = loginModel.Password } },
                "login");

            if (response == null || string.IsNullOrEmpty(response.AccessToken))
                throw new GraphQLClientException("Credenciales inválidas");

            await js.InvokeVoidAsync("sessionStorage.setItem", "accessToken", response.AccessToken);
            return response.User;
        });
    }

    public async Task<Result<UserGQL>> GetCurrentUserAsync()
    {
        const string query = """
            query CurrentUser {
                currentUser { id name email roles }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var user = await graphQL.ExecuteAsync<UserGQL>(query, null, "currentUser");
            return user!;
        });
    }

    public async Task Logout()
    {
        const string mutation = """
            mutation RevokeToken {
                revokeToken
            }
            """;

        try
        {
            await graphQL.ExecuteAsync<bool>(mutation, null, "revokeToken");
        }
        catch { }

        await js.InvokeVoidAsync("sessionStorage.clear");
        await js.InvokeVoidAsync("location.reload");
    }

    public async Task<string?> GetToken()
        => await js.InvokeAsync<string>("sessionStorage.getItem", "accessToken");
}
