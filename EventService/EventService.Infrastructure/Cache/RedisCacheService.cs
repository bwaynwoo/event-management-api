using System.Text.Json;
using EventService.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EventService.Infrastructure.Cache;

public sealed class RedisCacheService(
    IConnectionMultiplexer redis,
    IOptions<RedisOptions> options,
    ILogger<RedisCacheService> logger)
    : ICacheService
{
    private readonly IDatabase _db = redis.GetDatabase();
    private readonly RedisOptions _options = options.Value;

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            if (!value.HasValue)
                return null;

            return JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis GET failed for key {Key}, falling back to database", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
        where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, json, ttl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis SET failed for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis DEL failed for key {Key}", key);
        }
    }
}
