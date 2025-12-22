using StackExchange.Redis;
using System.Text.Json;

namespace GixatBackend.Modules.Common.Services.Redis;

internal sealed class RedisCacheService : IRedisCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _database = redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var value = await _database.StringGetAsync(key).ConfigureAwait(false);
        
        if (!value.HasValue)
        {
            return null;
        }

        return JsonSerializer.Deserialize<T>(value.ToString());
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        var serialized = JsonSerializer.Serialize(value);
        
        if (expiry.HasValue)
        {
            await _database.StringSetAsync(key, serialized, expiry.Value).ConfigureAwait(false);
        }
        else
        {
            await _database.StringSetAsync(key, serialized).ConfigureAwait(false);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        await _database.KeyDeleteAsync(key).ConfigureAwait(false);
    }
}
