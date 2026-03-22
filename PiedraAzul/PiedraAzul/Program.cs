#region NameSpaces
using Lucene.Net.Index;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PiedraAzul.ApplicationServices.Services;
using PiedraAzul.Client.Extensions;
using PiedraAzul.Components;
using PiedraAzul.Data;
using PiedraAzul.Extensions;
using PiedraAzul.GrpcServices;
using PiedraAzul.Seeders;
#endregion

var builder = WebApplication.CreateBuilder(args);

var isEf = args.Any(a => a.Contains("ef", StringComparison.OrdinalIgnoreCase));
IndexWriter? writer = null;
if (!isEf)
{
    builder.Services.AddLucene(builder.Configuration["LuceneIndexPath"] ?? "lucene_index", writer);
}

#region Mappers
builder.Services.AddMappers();
#endregion

#region DbContext
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion

#region JWTAndRefreshToken
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
#endregion

#region Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IPatientService, PatientService>();
#endregion
#region ClientServices
builder.Services.AddClientServer(builder.Configuration["GrpcUrl"] ?? "https://localhost:7128");
#endregion


#region AuthenticationAndAuthorization
builder.Services.AddAuthenticationAndAuthorization(builder);

builder.Services.AddIdentityApiEndpoints<ApplicationUser>(
    opts =>
    {
        opts.User.RequireUniqueEmail = false;
        opts.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();


builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;

    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
});
#endregion

#region RazorComponents
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(opt =>
    {
        opt.DetailedErrors = true;
    })
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

#region UI
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(PiedraAzul.Client._Imports).Assembly);
#endregion

#region AuthenticationAndAuthorization
app.UseAuthentication();
app.UseAuthorization();
#endregion

#region Middleware
app.UseHttpsRedirection();
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();
#endregion

#region gRPCWeb
app.UseGrpcWeb();
#endregion
#region gRPCServices
app.MapGrpcService<PiedraAzul.GrpcServices.GrpcAuth>().EnableGrpcWeb();
app.MapGrpcService<PiedraAzul.GrpcServices.GrpcAvailability>().EnableGrpcWeb();
app.MapGrpcService<PiedraAzul.GrpcServices.GrpcAppointment>().EnableGrpcWeb();
app.MapGrpcService<PiedraAzul.GrpcServices.GrpcDoctor>().EnableGrpcWeb();
app.MapGrpcService<PiedraAzul.GrpcServices.GrpcPatient>().EnableGrpcWeb();
#endregion

#region Dispose
if (writer != null)
{
    app.Lifetime.ApplicationStopping.Register(() => writer.Dispose());
}
#endregion

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    var usermanager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    await DbSeeder.SeedAsync(context, usermanager);
}

app.Run();
