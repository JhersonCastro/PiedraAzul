using Microsoft.JSInterop;

namespace PiedraAzul.Client.Services.AuthServices;

public interface ITokenService
{
    Task<string?> GetAccessTokenAsync();
    Task SetAccessTokenAsync(string token);
    Task<string?> RefreshTokenAsync();
    Task ClearAsync();
}

public class TokenService : ITokenService
{
    private readonly IJSRuntime js;
    private readonly RefreshAuthClient refreshClient;

    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    public TokenService(IJSRuntime js, RefreshAuthClient refreshClient)
    {
        this.js = js;
        this.refreshClient = refreshClient;
    }

    public async Task<string?> GetAccessTokenAsync()
        => await js.InvokeAsync<string>("sessionStorage.getItem", "accessToken");

    public async Task SetAccessTokenAsync(string token)
        => await js.InvokeVoidAsync("sessionStorage.setItem", "accessToken", token);

    public async Task ClearAsync()
        => await js.InvokeVoidAsync("sessionStorage.clear");

    public async Task<string?> RefreshTokenAsync()
    {
        await RefreshLock.WaitAsync();
        try
        {
            var newToken = await refreshClient.RefreshTokenAsync();

            if (string.IsNullOrWhiteSpace(newToken))
            {
                await ClearAsync();
                return null;
            }

            await SetAccessTokenAsync(newToken);
            return newToken;
        }
        catch
        {
            await ClearAsync();
            return null;
        }
        finally
        {
            RefreshLock.Release();
        }
    }
}
