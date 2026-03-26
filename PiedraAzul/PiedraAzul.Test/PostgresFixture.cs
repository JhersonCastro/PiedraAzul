using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using PiedraAzul.ApplicationServices.Services;
using PiedraAzul.Data;
using PiedraAzul.Data.Models;

public class PostgresFixture : IAsyncLifetime
{
    public IServiceProvider ServiceProvider { get; private set; } = null!;
    public IDbContextFactory<AppDbContext> DbContextFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        var dbName = $"TestDb_{Guid.NewGuid()}";

        var root = new InMemoryDatabaseRoot();

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseInMemoryDatabase(dbName, root);
        });

        services.AddDbContextFactory<AppDbContext>(options =>
        {
            options.UseInMemoryDatabase(dbName, root);
        });

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireLowercase = false;
            options.Password.RequiredLength = 4;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<IUserService, UserService>();

        ServiceProvider = services.BuildServiceProvider();

        DbContextFactory = ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();

        using var scope = ServiceProvider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ctx.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}