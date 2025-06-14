using StackExchange.Redis;

namespace RedisCacheDemo.Services
{
    public class ProductService(IConnectionMultiplexer redis, DistributedLockService lockService)
    {
        private readonly IDatabase _redis = redis.GetDatabase(); // Redis cache database
        private readonly DistributedLockService _lockService = lockService;

        // Simulated in-memory database
        private readonly Dictionary<int, string> _fakeDb = new()
        {
            { 1, "Product A" },
            { 2, "Product B" },
            { 3, "Product C" }
        };

        #region Get product using Cache-Aside pattern
        public async Task<string?> GetProductAsync(int id)
        {
            string key = $"product:{id}";
            var cached = await _redis.StringGetAsync(key);
            if (cached.HasValue)
                return cached!;

            if (_fakeDb.TryGetValue(id, out var product))
            {
                await _redis.StringSetAsync(key, product, TimeSpan.FromMinutes(5)); // Cache the value
                return product;
            }

            return null; // Not found in DB
        }
        #endregion

        #region Write-through cache update for product
        public async Task AddOrUpdateProductAsync(int id, string name)
        {
            _fakeDb[id] = name; // Update simulated DB
            await _redis.StringSetAsync($"product:{id}", name, TimeSpan.FromMinutes(5)); // Sync cache
        }
        #endregion

        #region Distributed locking & Cache-Invalidation Strategy
        public async Task<bool> UpdateProductWithLockAsync(int id, string value)
        {
            var lockKey = $"lock:product:{id}";
            var cacheKey = $"product:{id}";
            var lockValue = Guid.NewGuid().ToString();
            var acquired = await _lockService.AcquireLockAsync(lockKey, lockValue, TimeSpan.FromSeconds(10));

            if (!acquired) return false;

            try
            {
                // Update simulated DB
                _fakeDb[id] = value;

                // Manual Cache Invalidation
                await _redis.KeyDeleteAsync(cacheKey);
                
                return true;
            }
            finally
            {
                await _lockService.ReleaseLockAsync(lockKey, lockValue);
            }
        }
        #endregion
    }
}
