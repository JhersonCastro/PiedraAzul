using Microsoft.JSInterop;
using Shared.Grpc;
using System.Net;
using System.Net.Http.Headers;


namespace PiedraAzul.Client.DelegatingHandlers;

public class HttpDelegatingHandler : DelegatingHandler
{
    private readonly IJSRuntime js;
    private readonly AuthService.AuthServiceClient authClient;

    public HttpDelegatingHandler(
        IJSRuntime js,
        AuthService.AuthServiceClient authClient)
    {
        this.js = js;
        this.authClient = authClient;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var accessToken = await js.InvokeAsync<string>("localStorage.getItem", "accessToken");

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        // Intentar refrescar token
        var newToken = await RefreshTokenAsync();

        if (string.IsNullOrWhiteSpace(newToken))
            return response;

        // Clonar request antes de reenviarlo
        var newRequest = await CloneHttpRequestMessageAsync(request);

        newRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", newToken);

        return await base.SendAsync(newRequest, cancellationToken);
    }

    private async Task<string?> RefreshTokenAsync()
    {
        try
        {
            var refreshToken =
                await js.InvokeAsync<string>("localStorage.getItem", "refreshToken");

            if (string.IsNullOrWhiteSpace(refreshToken))
                return null;

            var response = await authClient.RefreshTokenAsync(
                new RefreshTokenRequest
                {
                    RefreshToken = refreshToken
                });

            if (string.IsNullOrWhiteSpace(response.AccessToken))
                return null;

            await js.InvokeVoidAsync("localStorage.setItem", "accessToken", response.AccessToken);
            await js.InvokeVoidAsync("localStorage.setItem", "refreshToken", response.RefreshToken);

            return response.AccessToken;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content != null)
        {
            var ms = new MemoryStream();
            await request.Content.CopyToAsync(ms);
            ms.Position = 0;

            clone.Content = new StreamContent(ms);

            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.Add(header.Key, header.Value);
            }
        }

        return clone;
    }
}