using Microsoft.JSInterop;
using Shared.Grpc;

namespace PiedraAzul.Client.Services;

public class AuthService
{
    private readonly AuthServiceProto.AuthServiceProtoClient authClient;
    private readonly IJSRuntime js;

    public AuthService(AuthServiceProto.AuthServiceProtoClient authClient, IJSRuntime js)
    {
        this.authClient = authClient;
        this.js = js;
    }

    public async Task<bool> Login(string email, string password)
    {
        var response = await authClient.LoginAsync(new LoginRequest
        {
            Email = email,
            Password = password
        });

        if (response == null || string.IsNullOrEmpty(response.AccessToken))
            return false;

        await js.InvokeVoidAsync("localStorage.setItem", "authToken", response.AccessToken);
        await js.InvokeVoidAsync("localStorage.setItem", "authType", response.Type);

        return true;
    }

    public async Task Logout()
    {
        await js.InvokeVoidAsync("localStorage.removeItem", "authToken");
        await js.InvokeVoidAsync("localStorage.removeItem", "authType");
    }

    public async Task<string?> GetToken()
    {
        return await js.InvokeAsync<string>("localStorage.getItem", "authToken");
    }
}