using Microsoft.JSInterop;
using Shared.Grpc;

namespace PiedraAzul.Client.Services.AuthServices
{
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

        private static SemaphoreSlim _refreshLock = new(1, 1);

        public TokenService(IJSRuntime js, RefreshAuthClient refreshClient)
        {
            this.js = js;
            this.refreshClient = refreshClient;
        }

        public async Task<string?> GetAccessTokenAsync()
        {
            return await js.InvokeAsync<string>("sessionStorage.getItem", "accessToken");
        }

        public async Task SetAccessTokenAsync(string token)
        {
            await js.InvokeVoidAsync("sessionStorage.setItem", "accessToken", token);
        }

        public async Task ClearAsync()
        {
            await js.InvokeVoidAsync("sessionStorage.clear");
        }

        public async Task<string?> RefreshTokenAsync()
        {
            await _refreshLock.WaitAsync();
            try
            {
                var response = await refreshClient.Client.RefreshTokenAsync(new RefreshTokenRequest());

                if (string.IsNullOrWhiteSpace(response.AccessToken))
                    return null;

                await SetAccessTokenAsync(response.AccessToken);

                return response.AccessToken;
            }
            catch
            {
                await ClearAsync();
                return null;
            }
            finally
            {
                _refreshLock.Release();
            }
        }
    }
}
