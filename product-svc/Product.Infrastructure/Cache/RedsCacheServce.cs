using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Product.Infrastructure.Cache;

public class RedsCacheServce
{
    private readonly IDistributedCache _cache;
    public RedsCacheServce(IDistributedCache cache) => _cache = cache;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var json = await _cache.GetStringAsync(key, ct);
        return json is null ? default : JsonSerializer.Deserialize<T>(json);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value);
        return _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct);
    }

    public Task RemoveAsync(string key, CancellationToken ct = default) => _cache.RemoveAsync(key, ct);
}

