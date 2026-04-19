using Microsoft.AspNetCore.Components;
using PiedraAzul.Client.Models;
using PiedraAzul.Client.Models.GraphQL;
using PiedraAzul.Client.Models.UserProfiles;
using PiedraAzul.Client.Services.GraphQLServices;
using PiedraAzul.Client.Services.Wrappers;

namespace PiedraAzul.Client.Services.AuthServices;

public class AuthenticationService(GraphQLHttpClient graphQL, NavigationManager nav)
{
    public async Task<Result<UserGQL>> RegisterAsync(RegisterModel registerModel, string role)
    {
        const string mutation = """
            mutation Register($input: RegisterInput!) {
                register(input: $input) { id name email roles avatarUrl }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var user = await graphQL.ExecuteAsync<UserGQL>(
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

            return user!;
        });
    }

    public async Task<Result<UserGQL>> LoginAsync(LoginModel loginModel)
    {
        const string mutation = """
            mutation Login($input: LoginInput!) {
                login(input: $input) { id name email roles avatarUrl }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var user = await graphQL.ExecuteAsync<UserGQL>(
                mutation,
                new { input = new { email = loginModel.Login, password = loginModel.Password } },
                "login");

            if (user is null)
                throw new GraphQLClientException("Credenciales inválidas");

            return user;
        });
    }

    public async Task<Result<UserGQL>> GetCurrentUserAsync()
    {
        const string query = """
            query CurrentUser {
                currentUser { id name email roles avatarUrl }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var user = await graphQL.ExecuteAsync<UserGQL>(query, null, "currentUser");
            return user!;
        });
    }

    public async Task<Result<UserGQL>> UpdateProfileAsync(string name, string? avatarUrl)
    {
        const string mutation = """
            mutation UpdateProfile($input: UpdateProfileInput!) {
                updateProfile(input: $input) { id name email roles avatarUrl }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var user = await graphQL.ExecuteAsync<UserGQL>(
                mutation,
                new { input = new { name, avatarUrl } },
                "updateProfile");
            return user!;
        });
    }

    public async Task Logout()
    {
        const string mutation = """
            mutation Logout {
                logout
            }
            """;

        try { await graphQL.ExecuteAsync<bool>(mutation, null, "logout"); }
        catch { }

        nav.NavigateTo("/", forceLoad: true);
    }
}
