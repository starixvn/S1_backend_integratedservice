
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.Library
{
    public interface ICacheHelper
    {
        TItem TryGetOrCreate<TItem>(string key, TimeSpan timeSpan, Func<TItem> factory, RedisDataBase redisDataBase = RedisDataBase.Tp);
    }
}
