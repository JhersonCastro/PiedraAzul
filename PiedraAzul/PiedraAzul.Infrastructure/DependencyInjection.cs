using Fido2NetLib;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Infrastructure.DataProtection;
using PiedraAzul.Infrastructure.Services;
using PiedraAzul.Domain.Repositories;
using PiedraAzul.Infrastructure.Caching;
using PiedraAzul.Infrastructure.Identity;
using PiedraAzul.Infrastructure.Persistence;
using PiedraAzul.Infrastructure.Persistence.Repositories;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Builder;
using PiedraAzul.Infrastructure.BackgroundJobs;

namespace PiedraAzul.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
    {
        Console.WriteLine("INFRA OK");

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Auditoría — interceptor que captura todo cambio en BD.
        services.AddHttpContextAccessor();
        services.AddSingleton<Audit.AuditSaveChangesInterceptor>();

        // DbContext (Scoped - para uso general)
        services.AddDbContext<AppDbContext>((sp, options) =>
            options.UseNpgsql(connectionString)
                   .AddInterceptors(sp.GetRequiredService<Audit.AuditSaveChangesInterceptor>()));

        // 🔐 Data Protection - persistir keys en PostgreSQL para sobrevivir deploys
        services.AddSingleton<PostgresXmlRepository>();
        services.AddDataProtection()
            .SetApplicationName("PiedraAzul");
        services.AddOptions<KeyManagementOptions>()
            .Configure<PostgresXmlRepository>((opts, repo) =>
                opts.XmlRepository = repo);

        // Identity
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddSignInManager()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddClaimsPrincipalFactory<ApplicationUserClaimsFactory>();

        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Password.RequiredLength = 6;
            options.Password.RequiredUniqueChars = 1;

            var lockoutConfig = configuration.GetSection("Security:Lockout");
            var maxFailedAttempts = lockoutConfig.GetValue<int>("MaxFailedAttempts", 5);
            var lockoutDurationMinutes = lockoutConfig.GetValue<int>("LockoutDurationMinutes", 15);

            options.Lockout.MaxFailedAccessAttempts = maxFailedAttempts;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(lockoutDurationMinutes);
            options.Lockout.AllowedForNewUsers = true;
        });

        // Repositories 
        services.Scan(scan => scan
            .FromAssembliesOf(typeof(AppointmentRepository))
            .AddClasses(classes => classes
                .Where(type => type.Name.EndsWith("Repository")))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Services
        services.Scan(scan => scan
            .FromAssembliesOf(typeof(AppDbContext))
            .AddClasses(classes => classes
                .Where(type => type.Name.EndsWith("Service")))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Caching
        services.AddMemoryCache();
        services.AddSingleton<ISlotCache, SlotCache>();
        // Cache de lectura (cache-aside con invalidación por tags). Singleton: los tags se comparten.
        services.AddSingleton<ICacheService, Caching.MemoryCacheService>();

        // Fido2 / Passkeys
        services.AddSingleton<IFido2>(_ =>
        {
            var origins = configuration.GetSection("Fido2:Origins").Get<string[]>()
                ?? ["https://localhost:7128"];
            return new Fido2(new Fido2Configuration
            {
                ServerDomain = configuration["Fido2:ServerDomain"] ?? "localhost",
                ServerName = configuration["Fido2:ServerName"] ?? "PiedraAzul",
                Origins = new HashSet<string>(origins),
                TimestampDriftTolerance = 300000
            });
        });


        // HangFire (Background Jobs)
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSQLiteStorage("hangfire_jobs.db"));

        services.AddHangfireServer();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Email Service
        services.AddScoped<IEmailService, EmailService>();

        // MFA Service
        services.AddScoped<IMFAService, MFAService>();

        // MFA Token Service
        services.AddScoped<IMFATokenService, MFATokenService>();

        // WhatsApp Service (Twilio — mismo Account que SMS)
        services.AddScoped<IWhatsAppService, WhatsAppService>();

        // Gemini AI (Consulta inteligente) — el servicio se auto-registra vía Scan
        services.AddHttpClient("Gemini");

        // SMS Service (Twilio)
        services.AddScoped<IMessageService, TwilioSmsService>();

        // Guest OTP Service
        services.AddScoped<IGuestOtpService, GuestOtpService>();

        // Audit Service (negocio)
        services.AddScoped<IAuditService, AuditService>();

        return services;
    }
    public static WebApplication UseInfraestructure(this WebApplication app)
    {
        var config = app.Services.GetRequiredService<IConfiguration>();
        var filter = new HangfireDashboardAuthorizationFilter(config);

        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [filter],
            // Permite que el dashboard funcione detrás de un proxy inverso (MonsterASP)
            DashboardTitle = "Piedra Azul — Jobs",
        });

        return app;
    }
}