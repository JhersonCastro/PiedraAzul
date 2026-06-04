using Microsoft.AspNetCore.Components;
using PiedraAzul.Client.Models;
using PiedraAzul.Client.Models.GraphQL;
using PiedraAzul.Client.Models.UserProfiles;
using PiedraAzul.Client.Services.GraphQLServices;
using PiedraAzul.Client.Services.Wrappers;

namespace PiedraAzul.Client.Services.AuthServices;

public class AuthenticationService(GraphQLHttpClient graphQL, NavigationManager nav)
{
    public async Task<Result<LoginResultModel>> RegisterAsync(RegisterModel registerModel, string role)
    {
        const string mutation = """
            mutation Register($input: RegisterInput!) {
                register(input: $input) {
                    user { id name email roles avatarUrl emailConfirmed }
                    loginToken
                }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var result = await graphQL.ExecuteAsync<LoginResultModel>(
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
                        documentType = registerModel.DocumentType.ToString(),        // CC/TI/CE/PA
                        gender = registerModel.Gender.ToString().ToUpperInvariant(), // MALE/FEMALE/OTHER
                        birthDate = registerModel.BirthDate.HasValue
                            ? DateTime.SpecifyKind(registerModel.BirthDate.Value, DateTimeKind.Utc)
                            : (DateTime?)null,
                        roles = new[] { role }
                    }
                },
                "register");

            if (result is null)
                throw new GraphQLClientException("No se pudo completar el registro");

            return result;
        });
    }

    public async Task<Result<LoginResultModel>> LoginAsync(LoginModel loginModel)
    {
        const string mutation = """
            mutation Login($input: LoginInput!) {
                login(input: $input) {
                    user { id name email roles avatarUrl emailConfirmed }
                    mfaRequired { mfaToken mfaMethod hasEmail }
                    loginToken
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

    public async Task<Result<LoginResultModel>> VerifyMFALoginAsync(string mfaToken, string otp)
    {
        const string mutation = """
            mutation VerifyMFALogin($input: VerifyMFALoginInput!) {
                verifyMFALogin(input: $input) {
                    user { id name email roles avatarUrl emailConfirmed }
                    loginToken
                }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var result = await graphQL.ExecuteAsync<LoginResultModel>(
                mutation,
                new { input = new { mfaToken, otp } },
                "verifyMFALogin");

            if (result is null)
                throw new GraphQLClientException("Verificación MFA inválida");

            return result;
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

    public Task Logout()
    {
        // Navegamos al endpoint HTTP directo para que Set-Cookie (clear) llegue
        // al browser sin importar si estamos en Server circuit o WASM.
        nav.NavigateTo("/auth/sign-out", forceLoad: true);
        return Task.CompletedTask;
    }

    public async Task<Result<LoginResultModel>> VerifyBackupCodeLoginAsync(string mfaToken, string backupCode)
    {
        const string mutation = """
            mutation VerifyBackupCodeLogin($input: VerifyBackupCodeLoginInput!) {
                verifyBackupCodeLogin(input: $input) {
                    user { id name email roles avatarUrl emailConfirmed }
                    loginToken
                }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var result = await graphQL.ExecuteAsync<LoginResultModel>(
                mutation,
                new { input = new { mfaToken, backupCode } },
                "verifyBackupCodeLogin");

            if (result is null)
                throw new GraphQLClientException("Código de recuperación inválido o ya utilizado");

            return result;
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
