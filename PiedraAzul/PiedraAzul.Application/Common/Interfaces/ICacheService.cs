namespace PiedraAzul.Application.Common.Interfaces;

/// <summary>
/// Cache de lectura (cache-aside) con invalidación por tags.
/// Acelera las consultas lentas a BD manteniendo la consistencia vía invalidación por evento.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Devuelve el valor cacheado o lo crea con <paramref name="factory"/> y lo guarda con
    /// el TTL y los tags indicados. Los tags permiten invalidar grupos de entradas a la vez.
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        string[]? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>Elimina una entrada puntual por su key (invalidación granular).</summary>
    void Remove(string key);

    /// <summary>Invalida de un golpe todas las entradas asociadas a un tag.</summary>
    void RemoveByTag(string tag);
}
