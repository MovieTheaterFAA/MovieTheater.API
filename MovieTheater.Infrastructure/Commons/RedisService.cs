using MovieTheater.Infrastructure.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace MovieTheater.Infrastructure.Commons
{
    public class RedisService : IRedisService
    {
        private readonly IConnectionMultiplexer _connection;
        private readonly IDatabase _database;

        public RedisService(IConnectionMultiplexer connection)
        {
            _connection = connection;
            _database = connection.GetDatabase();
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var value = await _database.StringGetAsync(key);
            if (value.IsNullOrEmpty) return default;
            return JsonSerializer.Deserialize<T>(value);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var json = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, json, expiration);
        }

        public async Task RemoveAsync(string key)
        {
            await _database.KeyDeleteAsync(key);
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return await _database.KeyExistsAsync(key);
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            foreach (var endpoint in _connection.GetEndPoints())
            {
                var server = _connection.GetServer(endpoint);
                var keys = server.Keys(pattern: $"{pattern}*").ToArray();
                if (keys.Any())
                    await _database.KeyDeleteAsync(keys);
            }
        }

        // New method to get keys by pattern
        public async Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern)
        {
            var keys = new List<string>();

            // Use Task.Run for the synchronous Keys operation
            await Task.Run(() =>
            {
                foreach (var endpoint in _connection.GetEndPoints())
                {
                    var server = _connection.GetServer(endpoint);
                    var redisKeys = server.Keys(pattern: $"{pattern}*").ToArray();

                    foreach (var redisKey in redisKeys)
                    {
                        keys.Add(redisKey.ToString());
                    }
                }
            });

            return keys;
        }
    }
}
