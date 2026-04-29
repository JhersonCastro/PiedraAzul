using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PiedraAzul.Infrastructure.Identity;
using PiedraAzul.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace PiedraAzul.Test;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("piedraazul_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public IServiceProvider ServiceProvider { get; private set; } = null!;
    public IDbContextFactory<AppDbContext> DbContextFactory { get; private set; } = null!;
    public IConfiguration Configuration { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var services = new ServiceCollection();
        var connectionString = _postgresContainer.GetConnectionString();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

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

        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "SuperSecretTestKeyThatIsLongEnoughToBeAValidHmacSha256KeyOnlyForTesting",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience"
            })
            .Build();

        services.AddSingleton(Configuration);

        ServiceProvider = services.BuildServiceProvider();
        DbContextFactory = ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();

        await using var scope = ServiceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();

        if (ServiceProvider is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else
            (ServiceProvider as IDisposable)?.Dispose();
    }
}
