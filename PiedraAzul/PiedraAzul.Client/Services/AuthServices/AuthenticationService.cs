using Grpc.Core;
using Microsoft.JSInterop;
using PiedraAzul.Client.Models;
using PiedraAzul.Client.Models.UserProfiles;
using PiedraAzul.Client.Services.Wrappers;
using Shared.Grpc;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PiedraAzul.Client.Services.AuthServices;

public class AuthenticationService
{
    private readonly AuthService.AuthServiceClient authClient;
    private readonly IJSRuntime js;

    public AuthenticationService(AuthService.AuthServiceClient authClient, IJSRuntime js)
    {
        this.authClient = authClient;
        this.js = js;
    }
    public async Task<Result<UserResponse>> RegisterAsync(RegisterModel registerModel, string role)
    {
        var result = await GrpcExecutor.Execute(async () => { 
            RegisterRequest registerRequest = new RegisterRequest
            {
                BirthDate = registerModel.BirthDate.HasValue ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(registerModel.BirthDate.Value.ToUniversalTime()) : null,
                Email = registerModel.Email ?? string.Empty,
                Gender = (GenderType)registerModel.Gender,
                IdentificationNumber = registerModel.Document,
                Name = registerModel.FullName,
                Password = registerModel.Password,
                Phone = registerModel.Phone,
                
            };
            registerRequest.Roles.Add(role);
            var response = await authClient.RegisterAsync(registerRequest);

            await js.InvokeVoidAsync("sessionStorage.setItem", "accessToken", response.AccessToken);

            return response.User;
        });
        return result;
    }
    public async Task<Result<UserResponse>> LoginAsync(LoginModel loginModel)
    {
        var result = await GrpcExecutor.Execute(async () =>
        {
            var response = await authClient.LoginAsync(new LoginRequest
            {
                Email = loginModel.Login,
                Password = loginModel.Password
            });

            if (response == null || string.IsNullOrEmpty(response.AccessToken))
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Credenciales inválidas"));

            await js.InvokeVoidAsync("sessionStorage.setItem", "accessToken", response.AccessToken);

            return response.User;
        });

        return result;
    }
    public async Task<Result<UserResponse>> GetCurrentUserAsync()
    {
        var result = await GrpcExecutor.Execute(async () =>
        {
            var response = await authClient.GetCurrentUserAsync(new Shared.Grpc.Empty());
            return response;
        });
        return result;
    }

    public async Task Logout()
    {
        
        try
        {
            await authClient.RevokeTokenAsync(new RevokeTokenRequest());
        }
        catch { }

        await js.InvokeVoidAsync("sessionStorage.clear");
        await js.InvokeVoidAsync("location.reload");
    }

    public async Task<string?> GetToken()
    {
        return await js.InvokeAsync<string>("sessionStorage.getItem", "accessToken");
    }
}