using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using PiedraAzul.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Shared.Grpc;

var builder = WebAssemblyHostBuilder.CreateDefault(args);


// gRPC Channel
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

// gRPC Services
builder.Services.AddScoped(sp =>
    new Greeter.GreeterClient(sp.GetRequiredService<GrpcChannel>()));


builder.Services.AddScoped<GreeterService>();


await builder.Build().RunAsync();
