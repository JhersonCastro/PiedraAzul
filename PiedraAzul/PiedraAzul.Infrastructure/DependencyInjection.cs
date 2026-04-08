using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Domain.Repositories;
using PiedraAzul.Infrastructure.Caching;
using PiedraAzul.Infrastructure.Identity;
using PiedraAzul.Infrastructure.Persistence;
using PiedraAzul.Infrastructure.Persistence.Repositories;
using PiedraAzul.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;

namespace PiedraAzul.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
    {
        Console.WriteLine("INFRA OK");

        // 🔹 DbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // 🔹 Identity (🔥 ESTO FALTABA)
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // 🔹 Repositories 
        services.Scan(scan => scan
            .FromAssembliesOf(typeof(AppointmentRepository))
            .AddClasses(classes => classes
                .Where(type => type.Name.EndsWith("Repository")))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // 🔹 Services
        services.Scan(scan => scan
            .FromAssembliesOf(typeof(AppDbContext))
            .AddClasses(classes => classes
                .Where(type => type.Name.EndsWith("Service")))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // 🔹 Caching
        services.AddMemoryCache();
        services.AddSingleton<ISlotCache, SlotCache>();

        // 🔹 Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}