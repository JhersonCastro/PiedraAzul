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
                register(input: $input) { id name email roles avatarUrl emailConfirmed }
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

    public async Task<Result<LoginResultModel>> LoginAsync(LoginModel loginModel)
    {
        const string mutation = """
            mutation Login($input: LoginInput!) {
                login(input: $input) {
                    user { id name email roles avatarUrl emailConfirmed }
                    mfaRequired { mfaToken mfaMethod hasEmail }
                }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var result = await graphQL.ExecuteAsync<LoginResultModel>(
                mutation,
                new { input = new { email = loginModel.Login, password = loginModel.Password } },
                "login");

            if (result is null)
                throw new GraphQLClientException("Credenciales inválidas");

            return result;
        });
    }

    public async Task<Result<UserGQL>> VerifyMFALoginAsync(string mfaToken, string otp)
    {
        const string mutation = """
            mutation VerifyMFALogin($input: VerifyMFALoginInput!) {
                verifyMFALogin(input: $input) { id name email roles avatarUrl emailConfirmed }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var user = await graphQL.ExecuteAsync<UserGQL>(
                mutation,
                new { input = new { mfaToken, otp } },
                "verifyMFALogin");

            if (user is null)
                throw new GraphQLClientException("Verificación MFA inválida");

            return user;
        });
    }

    public async Task<Result<UserGQL>> GetCurrentUserAsync()
    {
        const string query = """
            query CurrentUser {
                currentUser { id name email roles avatarUrl emailConfirmed }
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
                updateProfile(input: $input) { id name email roles avatarUrl emailConfirmed }
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

    public async Task<Result<UserGQL>> VerifyBackupCodeLoginAsync(string mfaToken, string backupCode)
    {
        const string mutation = """
            mutation VerifyBackupCodeLogin($input: VerifyBackupCodeLoginInput!) {
                verifyBackupCodeLogin(input: $input) { id name email roles avatarUrl emailConfirmed }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var user = await graphQL.ExecuteAsync<UserGQL>(
                mutation,
                new { input = new { mfaToken, backupCode } },
                "verifyBackupCodeLogin");

            if (user is null)
                throw new GraphQLClientException("Código de recuperación inválido o ya utilizado");

            return user;
        });
    }

    public async Task<Result<bool>> ResendMFACodeAsync(string mfaToken)
    {
        const string mutation = """
            mutation ResendMFACode($mfaToken: String!) {
                resendMFACode(mfaToken: $mfaToken)
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            try
            {
                var result = await graphQL.ExecuteAsync<bool>(
                    mutation,
                    new { mfaToken },
                    "resendMFACode");

                return result;
            }
            catch (GraphQLClientException ex) when (ex.Message.Contains("Demasiados"))
            {
                throw new GraphQLClientException("Demasiados reintentos. Espera 1 hora e intenta nuevamente.");
            }
            catch (GraphQLClientException ex) when (ex.Message.Contains("expirado"))
            {
                throw new GraphQLClientException("Tu sesión ha expirado. Inicia sesión nuevamente.");
            }
        });
    }
}
