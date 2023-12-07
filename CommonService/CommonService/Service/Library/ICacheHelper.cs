using CommonService.Service.Model.enums;

using StackExchange.Redis;

namespace CommonService.Service.Library
{
    public interface ICacheHelper
    {
        TItem TryGetOrCreate<TItem>(string key, TimeSpan timeSpan, Func<TItem> factory, RedisDataBase redisDataBase = RedisDataBase.Shared);

        Task<TItem> TryGetOrCreateAsync<TItem>(string key, TimeSpan timeSpan, Func<Task<TItem>> factory, RedisDataBase redisDataBase = RedisDataBase.Shared);

        Task<bool> StringSetAsync(string key, RedisValue value, TimeSpan? timeSpan, RedisDataBase redisDataBase = RedisDataBase.Shared);

        bool StringSet(string key, RedisValue value, TimeSpan? timeSpan, RedisDataBase redisDataBase = RedisDataBase.Shared);

        bool KeyExists(string key, RedisDataBase redisDataBase = RedisDataBase.Shared);

        Task<TItem> StringGetAsync<TItem>(string key, RedisDataBase redisDataBase = RedisDataBase.Shared);

        bool InsertList<TItem>(string key, TimeSpan? timeSpan, IEnumerable<TItem> list, RedisDataBase redisDataBase = RedisDataBase.Shared);

        List<TItem> GetListByRange<TItem>(string key, long startIndex, long endIndex, RedisDataBase redisDataBase = RedisDataBase.Shared);

        long GetListLength(string key, RedisDataBase redisDataBase = RedisDataBase.Shared);

        Task<bool> ListLeftPopAsync(string key, RedisDataBase redisDataBase = RedisDataBase.Shared);

        bool KeyDelete(string key, RedisDataBase redisDataBase = RedisDataBase.Shared);
    }
}