using Microsoft.JSInterop;
using System.Net.Http;
using System.Net.Http.Headers;
using Shared.Grpc;

namespace PiedraAzul.Client.DelegatingHandlers;

public class HttpDelegatingHandler : DelegatingHandler
{
    private readonly IJSRuntime js;
    private readonly AuthServiceProto.AuthServiceProtoClient authClient;

    public HttpDelegatingHandler(
        IJSRuntime js,
        AuthServiceProto.AuthServiceProtoClient authClient)
    {
        this.js = js;
        this.authClient = authClient;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // 1️⃣ agregar token
        var token = await js.InvokeAsync<string>("localStorage.getItem", "authToken");

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        // 2️⃣ enviar request
        var response = await base.SendAsync(request, cancellationToken);

        // 3️⃣ si expiró, refrescar vía gRPC
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var newToken = await RefreshTokenAsync();

            if (!string.IsNullOrEmpty(newToken))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", newToken);

                response = await base.SendAsync(request, cancellationToken);
            }
        }

        return response;
    }

    private async Task<string?> RefreshTokenAsync()
    {
        try
        {
            var response = await authClient.RefreshAsync(new RefreshRequest());

            if (string.IsNullOrEmpty(response.AccessToken))
                return null;

            await js.InvokeVoidAsync("localStorage.setItem", "authToken", response.AccessToken);

            return response.AccessToken;
        }
        catch
        {
            return null;
        }
    }
}