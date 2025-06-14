using StackExchange.Redis;
using System.Net;


namespace RedisCacheDemo.Middleware;

public class RedisRateLimitMiddleware(RequestDelegate next, IConnectionMultiplexer redis)
{
    private readonly RequestDelegate _next = next;
    private readonly IConnectionMultiplexer _redis = redis;
    private const int LIMIT = 5;
    private static readonly TimeSpan WINDOW = TimeSpan.FromMinutes(1);

    // Configure per-IP rate limiter: Allow 5 requests per 1 minute per IP
    public async Task InvokeAsync(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var _cache = _redis.GetDatabase();
        var key = $"ratelimit:{ip}";

        // Increment counter
        var current = await _cache.StringIncrementAsync(key);

        // Set expiration if first request
        if (current == 1)
        {
            await _cache.KeyExpireAsync(key, WINDOW);
        }

        if (current > LIMIT)
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers.RetryAfter = ((int)WINDOW.TotalSeconds).ToString();
            await context.Response.WriteAsync("Too Many Requests (Redis-Based Rate Limiting)");
            return;
        }

        await _next(context);
    }
}
