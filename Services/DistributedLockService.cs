using StackExchange.Redis;

namespace RedisCacheDemo.Services;

public class DistributedLockService
{
    private readonly IDatabase _redis;

    public DistributedLockService(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    public async Task<bool> AcquireLockAsync(string key, string value, TimeSpan expiry)
    {
        // SET key value NX PX expiry
        return await _redis.StringSetAsync(key, value, expiry, When.NotExists);
    }

    public async Task<bool> ReleaseLockAsync(string key, string value)
    {
        // Only release if current value matches (safe unlock)
        var script = @"
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('del', KEYS[1])
                else
                    return 0
                end";
        var result = (int)await _redis.ScriptEvaluateAsync(script, [key], [value]);
        return result == 1;
    }
}