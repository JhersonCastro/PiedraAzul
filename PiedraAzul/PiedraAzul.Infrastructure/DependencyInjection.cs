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

namespace PiedraAzul.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
    {
        Console.WriteLine("INFRA OK");

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // DbContext (Scoped - para uso general)
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

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

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Email Service
        services.AddScoped<IEmailService, EmailService>();

        // MFA Service
        services.AddScoped<IMFAService, MFAService>();

        // MFA Token Service
        services.AddScoped<IMFATokenService, MFATokenService>();

        // WhatsApp Service (Meta Cloud API)
        services.AddHttpClient("WhatsApp");
        services.AddScoped<IWhatsAppService, WhatsAppService>();

        // SMS Service (Twilio)
        services.AddScoped<IMessageService, TwilioSmsService>();

        // Guest OTP Service
        services.AddScoped<IGuestOtpService, GuestOtpService>();

        return services;
    }
}