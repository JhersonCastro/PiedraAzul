using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.AspNetCore.Components;
using Shared.Grpc;

namespace PiedraAzul.Client.Services.AuthServices;

public class RefreshAuthClient
{
    public AuthService.AuthServiceClient Client { get; }

    public RefreshAuthClient(NavigationManager navigation)
    {
        var baseAddress = navigation.BaseUri;

        var handler = new GrpcWebHandler(
            GrpcWebMode.GrpcWeb,
            new HttpClientHandler());

        var channel = GrpcChannel.ForAddress(baseAddress, new GrpcChannelOptions
        {
            HttpHandler = handler
        });

        Client = new AuthService.AuthServiceClient(channel);
    }
}