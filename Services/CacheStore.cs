using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace BikeCarHub.Services;

/// <summary>Cache abstraction so IMemoryCache can be swapped for Redis later.</summary>
public interface ICacheStore
{
    Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<Task<T>> factory) where T : class;
}

public sealed class MemoryCacheStore(IMemoryCache cache) : ICacheStore
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<Task<T>> factory) where T : class
    {
        if (cache.TryGetValue(key, out T? cached) && cached is not null) return cached;
        var gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            if (cache.TryGetValue(key, out cached) && cached is not null) return cached;
            var value = await factory();
            cache.Set(key, value, ttl);
            return value;
        }
        finally
        {
            gate.Release();
        }
    }
}
