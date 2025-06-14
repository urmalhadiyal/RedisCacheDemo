using RedisCacheDemo.Middleware;
using RedisCacheDemo.Services;
using StackExchange.Redis;

namespace RedisCacheDemo;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Redis connection multiplexer as singleton
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect("localhost:6379") // Change to match your Redis instance
        );

        // Add services to the container.
        builder.Services.AddSingleton<ProductService>();
        builder.Services.AddSingleton<DistributedLockService>();

        builder.Services.AddControllers();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        // Enable custom Redis-based rate limiting middleware
        app.UseMiddleware<RedisRateLimitMiddleware>();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
