using Newtonsoft.Json;

using StackExchange.Redis;

using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.Library
{
    public class RedisCacheHelper : ICacheHelper
    {
        private readonly IConnectionMultiplexer _multiplexer;
        private readonly ILogger<RedisCacheHelper> _logger;
        private readonly string[] _charsToRemove = { "[", "]", "/", " ", "," };

        public RedisCacheHelper(IConnectionMultiplexer multiplexer, ILogger<RedisCacheHelper> logger)
        {
            _multiplexer = multiplexer;
            _logger = logger;
        }

        public TItem TryGetOrCreate<TItem>(string key, TimeSpan timeSpan, Func<TItem> factory, RedisDataBase redisDataBase = RedisDataBase.Tp)
        {
            key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);

                if (!db.KeyExists(key))
                {
                    var result = factory();
                    if (result != null)
                    {
                        db.StringSet(key, JsonConvert.SerializeObject(result), timeSpan);
                    }

                    return result;
                }

                var json = db.StringGet(key);
                return json.HasValue ? JsonConvert.DeserializeObject<TItem>(json) : factory();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return factory();
            }
        }      

        private string RemoveSpecialChars(string key)
        {
            foreach (var c in _charsToRemove)
            {
                key = key.Replace(c, string.Empty);
            }

            return key;
        }
    }
}