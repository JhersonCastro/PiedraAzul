using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PiedraAzul.Client.DelegatingHandlers;
using PiedraAzul.Client.Services;
using PiedraAzul.Client.States;
using Shared.Grpc;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

#region Handlers

builder.Services.AddTransient<CookieHandler>();
builder.Services.AddScoped<HttpDelegatingHandler>();

#endregion


#endregion

#region gRPC CHANNELS

// 🔹 Canal para AUTH (sin handler para evitar loop)
builder.Services.AddScoped(sp =>
{
    var handler = new GrpcWebHandler(
        GrpcWebMode.GrpcWeb,
        new HttpClientHandler());

    return GrpcChannel.ForAddress(
        builder.HostEnvironment.BaseAddress,
        new GrpcChannelOptions
        {
            HttpHandler = handler
        });
});

// 🔹 Canal para APIs protegidas
builder.Services.AddScoped(sp =>
{
    var cookieHandler = sp.GetRequiredService<CookieHandler>();
    var authHandler = sp.GetRequiredService<HttpDelegatingHandler>();

    cookieHandler.InnerHandler = authHandler;
    authHandler.InnerHandler = new HttpClientHandler();

    var grpcHandler = new GrpcWebHandler(
        GrpcWebMode.GrpcWeb,
        cookieHandler);

    return GrpcChannel.ForAddress(
        builder.HostEnvironment.BaseAddress,
        new GrpcChannelOptions
        {
            HttpHandler = grpcHandler
        });
});

#endregion

#region gRPC CLIENTS



builder.Services.AddScoped(sp =>
    new AuthService.AuthServiceClient(
        sp.GetRequiredService<GrpcChannel>()));

#endregion
#region gRPC Services

builder.Services.AddScoped<AuthenticationService>();

#endregion
#region Auth

builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthStateProvider>();

builder.Services.AddAuthorizationCore();

#endregion

await builder.Build().RunAsync();