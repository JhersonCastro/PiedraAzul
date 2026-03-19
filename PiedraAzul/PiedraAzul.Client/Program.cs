using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PiedraAzul.Client.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddClientWasm(builder.HostEnvironment.BaseAddress);

await builder.Build().RunAsync();