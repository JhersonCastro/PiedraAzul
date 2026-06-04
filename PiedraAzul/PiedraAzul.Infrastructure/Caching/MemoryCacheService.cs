using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using PiedraAzul.Application.Common.Interfaces;

namespace PiedraAzul.Infrastructure.Caching;

/// <summary>
/// Cache de lectura sobre IMemoryCache. La invalidación por tags se emula con
/// CancellationChangeToken: cada tag tiene un CTS; al invalidarlo se cancelan
/// todas las entradas enlazadas a ese tag de un solo golpe.
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    // Un CancellationTokenSource por tag. Acotado por (#doctores + #especialidades).
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _tags = new();

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        string[]? tags = null,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out T? cached) && cached is not null)
            return cached;

        var value = await factory(cancellationToken);

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };

        if (tags is not null)
        {
            foreach (var tag in tags)
            {
                var cts = _tags.GetOrAdd(tag, _ => new CancellationTokenSource());
                options.AddExpirationToken(new CancellationChangeToken(cts.Token));
            }
        }

        _cache.Set(key, value, options);
        return value;
    }

    public void Remove(string key) => _cache.Remove(key);

    public void RemoveByTag(string tag)
    {
        if (_tags.TryRemove(tag, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }
}
