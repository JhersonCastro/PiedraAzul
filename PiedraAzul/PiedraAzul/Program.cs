#region NameSpaces
using PiedraAzul.Components;
using PiedraAzul.Extensions;
using PiedraAzul.Application;
using PiedraAzul.Infrastructure;
using PiedraAzul.Client.Extensions;
#endregion

var builder = WebApplication.CreateBuilder(args);

// 🔹 capas
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// 🔹 UI
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// 🔹 API stuff
builder.Services.AddSignalR();
builder.Services.AddGrpc();

//InteractivityAuto 
builder.Services.AddClientServer(builder.Configuration["GrpcUrl"] ?? "https://localhost:7128",
                                builder.Configuration["hubUrl"] ?? "https://localhost:7128");

// 🔹 Auth
builder.Services.AddAuth(builder.Configuration);

var app = builder.Build();
// middlewares
app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapStaticAssets();
app.UseGrpcWeb();

app.UseAuthentication();
app.UseAuthorization();


app.UseAntiforgery();

// endpoints
app.MapGrpcServices();
app.MapHubs();



app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(PiedraAzul.Client._Imports).Assembly);

// seed
await app.SeedAsync();

app.Run();