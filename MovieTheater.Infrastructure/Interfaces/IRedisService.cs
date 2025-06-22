namespace MovieTheater.Infrastructure.Interfaces
{
    public interface IRedisService
    {
        Task<bool> ExistsAsync(string key);
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
        Task RemoveByPatternAsync(string pattern);
    }
}
