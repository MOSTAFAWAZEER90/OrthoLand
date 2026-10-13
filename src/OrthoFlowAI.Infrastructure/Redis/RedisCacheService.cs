using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace OrthoFlowAI.Infrastructure.Redis;

public class RedisCacheService : IRedisCacheService, IDisposable
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisCacheService> _logger;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public RedisCacheService(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisCacheService> logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
    }

    private IDatabase GetDatabase() => _connectionMultiplexer.GetDatabase();

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var database = GetDatabase();
            var value = await database.StringGetAsync(key);
            if (value.IsNullOrEmpty)
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(value.ToString(), SerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from Redis for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        try
        {
            var database = GetDatabase();
            var serialized = JsonSerializer.Serialize(value, SerializerOptions);
            if (expiry.HasValue)
            {
                await database.StringSetAsync(key, serialized, expiry.Value);
            }
            else
            {
                await database.StringSetAsync(key, serialized);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in Redis for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            var database = GetDatabase();
            await database.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing key from Redis: {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var database = GetDatabase();
            return await database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking key existence in Redis: {Key}", key);
            return false;
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: $"*{pattern}*");

            foreach (var key in keys)
            {
                await GetDatabase().KeyDeleteAsync(key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing keys by pattern from Redis: {Pattern}", pattern);
        }
    }

    public void Dispose()
    {
        if (_connectionMultiplexer is not null && _connectionMultiplexer.IsConnected)
        {
            _connectionMultiplexer.Close();
            _connectionMultiplexer.Dispose();
        }
    }
}
