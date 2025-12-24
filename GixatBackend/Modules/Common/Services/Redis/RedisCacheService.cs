using StackExchange.Redis;
using System.Text.Json;

namespace GixatBackend.Modules.Common.Services.Redis;

internal sealed class RedisCacheService : IRedisCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _database = redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var value = await _database.StringGetAsync(key).ConfigureAwait(false);
            
            if (!value.HasValue)
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(value.ToString(), JsonOptions);
        }
        catch (RedisException)
        {
            // Log error but don't throw - cache failures shouldn't break the app
            return null;
        }
    }

    public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var value = await _database.StringGetAsync(key).ConfigureAwait(false);
            return value.HasValue ? value.ToString() : null;
        }
        catch (RedisException)
        {
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            var serialized = JsonSerializer.Serialize(value, JsonOptions);
            
            if (expiry.HasValue)
            {
                await _database.StringSetAsync(key, serialized, expiry.Value).ConfigureAwait(false);
            }
            else
            {
                await _database.StringSetAsync(key, serialized).ConfigureAwait(false);
            }
        }
        catch (RedisException)
        {
            // Log error but don't throw - cache failures shouldn't break the app
        }
    }

    public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            if (expiry.HasValue)
            {
                await _database.StringSetAsync(key, value, expiry.Value).ConfigureAwait(false);
            }
            else
            {
                await _database.StringSetAsync(key, value).ConfigureAwait(false);
            }
        }
        catch (RedisException)
        {
            // Log error but don't throw
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            return await _database.KeyExistsAsync(key).ConfigureAwait(false);
        }
        catch (RedisException)
        {
            return false;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        
        try
        {
            await _database.KeyDeleteAsync(key).ConfigureAwait(false);
        }
        catch (RedisException)
        {
            // Log error but don't throw
        }
    }

    public async Task<bool> RemoveAsync(string[] keys, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);
        
        if (keys.Length == 0)
        {
            return false;
        }

        try
        {
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            var deleted = await _database.KeyDeleteAsync(redisKeys).ConfigureAwait(false);
            return deleted > 0;
        }
        catch (RedisException)
        {
            return false;
        }
    }

    public async Task<TimeSpan?> GetTimeToLiveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            return await _database.KeyTimeToLiveAsync(key).ConfigureAwait(false);
        }
        catch (RedisException)
        {
            return null;
        }
    }

    public async Task<bool> SetExpiryAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            return await _database.KeyExpireAsync(key, expiry).ConfigureAwait(false);
        }
        catch (RedisException)
        {
            return false;
        }
    }

    public Task<bool> IsConnectedAsync()
    {
        try
        {
            return Task.FromResult(_redis.IsConnected);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}
