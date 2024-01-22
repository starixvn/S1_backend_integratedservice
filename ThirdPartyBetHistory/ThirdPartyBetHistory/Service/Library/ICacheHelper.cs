
using StackExchange.Redis;

using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.Library
{
    public interface ICacheHelper
    {
        TItem TryGetOrCreate<TItem>(string key, TimeSpan timeSpan, Func<TItem> factory, RedisDataBase redisDataBase = RedisDataBase.Tp);

        string StringGet(string key, RedisDataBase redisDataBase = RedisDataBase.Tp);

        bool StringSet(string key, RedisValue value, TimeSpan? timeSpan = null, RedisDataBase redisDataBase = RedisDataBase.Tp);
    }
}
