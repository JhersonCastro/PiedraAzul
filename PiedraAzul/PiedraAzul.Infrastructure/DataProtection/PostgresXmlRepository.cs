using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using PiedraAzul.Infrastructure.Persistence;
using System.Xml.Linq;

namespace PiedraAzul.Infrastructure.DataProtection;

/// <summary>
/// Persiste las claves de Data Protection en PostgreSQL para que sobrevivan
/// restarts y deploys sin invalidar las cookies de autenticación existentes.
/// Usa IServiceScopeFactory para crear scopes propios sin conflictos de lifetime.
/// </summary>
public class PostgresXmlRepository : IXmlRepository
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PostgresXmlRepository(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return context.DataProtectionKeys
            .Select(k => k.Xml)
            .ToList()
            .Select(XElement.Parse)
            .ToList()
            .AsReadOnly();
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        context.DataProtectionKeys.Add(new DataProtectionKey
        {
            FriendlyName = friendlyName,
            Xml = element.ToString()
        });

        context.SaveChanges();
    }
}
