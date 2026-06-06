#region NameSpaces
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http.Features;
using PiedraAzul.Client.Services.GraphQLServices;
using PiedraAzul.Components;
using PiedraAzul.Extensions;
using PiedraAzul.Application;
using PiedraAzul.Infrastructure;
using PiedraAzul.Client.Extensions;
using PiedraAzul.Services;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.RealTime;
#endregion

var builder = WebApplication.CreateBuilder(args);

// 🔹 Kestrel - Allow larger file uploads
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
});

// 🔹 Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
#if Windows
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.AddEventLog();
}
#endif

// 🔹 capas
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// 🔹 UI
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// 🔹 Hangfire - Background jobs
builder.Services.AddScoped<PiedraAzul.Application.Common.Interfaces.IBackgroundNotificationService,
    PiedraAzul.Infrastructure.BackgroundJobs.BackgroundNotificationService>();

// 🔹 API stuff
builder.Services.AddScoped<IAppointmentNotifier, AppointmentNotifier>();
builder.Services.AddSignalR();
builder.Services.AddPiedraAzulGraphQL();
builder.Services.AddHttpContextAccessor();

// 🔹 Forwarded Headers - para obtener el IP real del cliente detrás del proxy (X-Forwarded-For)
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    // Confiar en el proxy del hosting (no conocemos su IP fija).
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// 🔹 Form Options - Allow larger file uploads
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB
});

// InteractivityAuto – registers shared client services + PersistentAuthenticationStateProvider

var graphqlUrl = builder.Configuration["GraphQLUrl"] ?? "https://localhost:7128";
var hubUrl = builder.Configuration["hubUrl"] ?? "https://localhost:7128";
builder.Services.AddClientServer(graphqlUrl, hubUrl);

// Override auth state provider for server-side: reads from HttpContext, persists to WASM
builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();

// IP real del cliente para auditoría: el servidor lee el HttpContext (override del Null de WASM)
builder.Services.AddScoped<PiedraAzul.Client.Services.IClientIpResolver, PiedraAzul.Services.HttpContextClientIpResolver>();

// Modo UI: el servidor lee la cookie uiMode del HttpContext para el prerender SSR (override del Null de WASM)
builder.Services.AddScoped<PiedraAzul.Client.Services.IUIModeReader, PiedraAzul.Services.HttpContextUIModeReader>();

// Tours: el servidor lee las cookies tourSeen_{id} para que WASM no intente mostrar tours ya vistos
builder.Services.AddScoped<PiedraAzul.Client.Services.ITourSeenReader, PiedraAzul.Services.HttpContextTourSeenReader>();

// Override GraphQL client for SSR: forwards the incoming request cookie + IP real del cliente
builder.Services.AddScoped<GraphQLHttpClient>(sp =>
{
    var accessor = sp.GetRequiredService<IHttpContextAccessor>();
    var clientIp = sp.GetRequiredService<PiedraAzul.Client.Services.ClientIpState>();
    var handler = new CookieForwardingHandler(accessor, clientIp);
    return new GraphQLHttpClient(new HttpClient(handler) { BaseAddress = new Uri(graphqlUrl) });
});

// 🔹 Login token (one-time session tokens para apply-session)
builder.Services.AddSingleton<ILoginTokenService, LoginTokenService>();

// 🔹 Auth
builder.Services.AddAuth(builder.Configuration);

var app = builder.Build();

// middlewares
// Forwarded headers primero: reescribe RemoteIpAddress con el IP real del cliente (proxy).
app.UseForwardedHeaders();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapStaticAssets();

// 🔹 Security Headers (minimal)
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

// 🔹 Hangfire Dashboard (solo en desarrollo)
app.UseInfraestructure();

app.UseAntiforgery();

// 🔹 CSRF Protection Middleware - Validates origin for GraphQL
app.UseMiddleware<PiedraAzul.Middleware.GraphQLCSRFMiddleware>();

// 🔹 Rate Limiting Middleware - Auth operations only
app.UseMiddleware<PiedraAzul.Middleware.AuthRateLimitMiddleware>();

// endpoints
app.MapGraphQLEndpoint();
app.MapHubs();
app.MapApiEndpoints();
app.MapWhatsAppWebhook();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(PiedraAzul.Client._Imports).Assembly);

// seed
await app.SeedAsync();

app.Run();
