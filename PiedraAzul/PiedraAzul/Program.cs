#region NameSpaces
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PiedraAzul.ApplicationServices.Services;
using PiedraAzul.Components;
using PiedraAzul.Data;
using System.Security.Claims;
using System.Text;
#endregion

var builder = WebApplication.CreateBuilder(args);

#region Lucene.Net
// Config
var luceneVersion = LuceneVersion.LUCENE_48;
var indexPath = builder.Configuration["LuceneIndexPath"] ?? "lucene_index";

var dir = FSDirectory.Open(indexPath);
var analyzer = new StandardAnalyzer(luceneVersion);
var indexConfig = new IndexWriterConfig(luceneVersion, analyzer);
var writer = new IndexWriter(dir, indexConfig);

// Services
builder.Services.AddSingleton<Analyzer>(analyzer);
builder.Services.AddSingleton<IndexWriter>(writer);

#endregion

#region Mapperly

#endregion

#region DbContext
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion

#region JWTAndRefreshToken
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<RefreshTokenService, RefreshTokenService>();
#endregion

#region AuthenticationAndAuthorization
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(5),
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),

        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.Name
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            ctx.Request.Cookies.TryGetValue("accessToken", out var accessToken);
            if (!string.IsNullOrEmpty(accessToken))
                ctx.Token = accessToken;

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddIdentityApiEndpoints<ApplicationUser>(
    opts =>
    {
        opts.User.RequireUniqueEmail = true;
        opts.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();
#endregion

#region RazorComponents
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
#endregion

#region gRPC
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = true;
});
#endregion

var app = builder.Build();

#region DevelopRuleSet
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
#endregion

#region RoleCreation
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "Admin", "Doctor", "Patient" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}
#endregion

#region Middleware
app.UseHttpsRedirection();
app.UseAntiforgery();
#endregion

#region UI
app.MapStaticAssets();
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(PiedraAzul.Client._Imports).Assembly);
#endregion

#region AuthenticationAndAuthorization
app.UseAuthentication();
app.UseAuthorization();
#endregion

#region gRPCWeb
app.UseGrpcWeb();
#endregion
#region gRPCServices

#endregion

app.Lifetime.ApplicationStopping.Register(() => writer.Dispose());
app.Run();
