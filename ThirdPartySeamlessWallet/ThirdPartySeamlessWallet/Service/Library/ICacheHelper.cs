using StackExchange.Redis;

using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Library
{
    public interface ICacheHelper
    {
        TItem TryGetOrCreate<TItem>(string key, TimeSpan timeSpan, Func<TItem> factory,
            RedisDataBase redisDataBase = RedisDataBase.Tp);

        Task<TItem> TryGetOrCreateAsync<TItem>(string key, TimeSpan? timeSpan, Func<Task<TItem>> factory,
            RedisDataBase redisDataBase = RedisDataBase.Tp);

        Task<bool> StringFireAndForgetSetAsync(string key, RedisValue value, TimeSpan? timeSpan,
                RedisDataBase redisDataBase = RedisDataBase.Tp);

        bool StringSet(string key, RedisValue value, TimeSpan? timeSpan,
            RedisDataBase redisDataBase = RedisDataBase.Tp);

        bool KeyExists(string key, RedisDataBase redisDataBase = RedisDataBase.Tp);

        Task<TItem> StringGetAsync<TItem>(string key, RedisDataBase redisDataBase = RedisDataBase.Tp);
        List<TItem> GetCacheList<TItem>(IEnumerable<string> keys, RedisDataBase redisDataBase = RedisDataBase.Tp);

        bool SetCacheList<TItem>(Dictionary<string, TItem> dataList, TimeSpan? timeSpan,
            RedisDataBase redisDataBase = RedisDataBase.Tp);

        bool InsertList<TItem>(string key, TimeSpan? timeSpan, IEnumerable<TItem> list,
            RedisDataBase redisDataBase = RedisDataBase.Tp);

        List<TItem> GetListByRange<TItem>(string key, long startIndex, long endIndex,
            RedisDataBase redisDataBase = RedisDataBase.Tp);

        long GetListLength(string key, RedisDataBase redisDataBase = RedisDataBase.Tp);

        Task<bool> KeyDeleteAsync(string key, RedisDataBase redisDataBase = RedisDataBase.Tp);
    }
}
