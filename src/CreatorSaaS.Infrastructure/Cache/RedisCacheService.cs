using CreatorSaaS.Core.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace CreatorSaaS.Infrastructure.Cache;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);

            if (!value.HasValue)
                return default;

            return JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache get failed for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var serialized = JsonSerializer.Serialize(value);
            await db.StringSetAsync(key, serialized, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache set failed for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache remove failed for key: {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            return await db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache exists check failed for key: {Key}", key);
            return false;
        }
    }
}
