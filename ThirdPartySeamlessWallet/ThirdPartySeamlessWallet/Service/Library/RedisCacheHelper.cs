using Newtonsoft.Json;

using StackExchange.Redis;
using System.Diagnostics;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Library
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

        public TItem TryGetOrCreate<TItem>(string key, TimeSpan timeSpan, Func<TItem> factory,
            RedisDataBase redisDataBase = RedisDataBase.Tp)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);

                var keyExistStopWatch = new Stopwatch();
                keyExistStopWatch.Start();

                var isKeyExists = !db.KeyExists(key);
                keyExistStopWatch.Stop();

                if (keyExistStopWatch.ElapsedMilliseconds > 1000)
                {
                    _logger.LogInformation($"KeyExist-{key}: {stopWatch.ElapsedMilliseconds}");
                }

                if (isKeyExists)
                {
                    var dbStopWatch = new Stopwatch();
                    dbStopWatch.Start();

                    var result = factory();
                    if (result != null)
                    {
                        dbStopWatch.Stop();
                        if (dbStopWatch.ElapsedMilliseconds > 2000)
                        {
                            _logger.LogInformation($"DB-{key}: {stopWatch.ElapsedMilliseconds}");
                        }

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
            finally
            {
                stopWatch.Stop();
                if (stopWatch.ElapsedMilliseconds > 2000)
                {
                    _logger.LogInformation($"{key}: {stopWatch.ElapsedMilliseconds}");
                }
            }
        }

        public async Task<TItem> TryGetOrCreateAsync<TItem>(string key, TimeSpan? timeSpan, Func<Task<TItem>> factory,
            RedisDataBase redisDataBase = RedisDataBase.Tp)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);

                var keyExistStopWatch = new Stopwatch();
                keyExistStopWatch.Start();

                var isKeyExists = !db.KeyExists(key);
                keyExistStopWatch.Stop();

                if (keyExistStopWatch.ElapsedMilliseconds > 1000)
                {
                    _logger.LogInformation($"KeyExist-{key}: {stopWatch.ElapsedMilliseconds}");
                }

                if (isKeyExists)
                {
                    var dbStopWatch = new Stopwatch();
                    dbStopWatch.Start();

                    var result = await factory();
                    if (result != null)
                    {
                        dbStopWatch.Stop();
                        if (dbStopWatch.ElapsedMilliseconds > 2000)
                        {
                            _logger.LogInformation($"DB-{key}: {stopWatch.ElapsedMilliseconds}");
                        }

                        await db.StringSetAsync(key, JsonConvert.SerializeObject(result), timeSpan);
                    }

                    return result;
                }

                var json = db.StringGet(key);
                return json.HasValue ? JsonConvert.DeserializeObject<TItem>(json) : await factory();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return await factory();
            }
            finally
            {
                stopWatch.Stop();
                if (stopWatch.ElapsedMilliseconds > 2000)
                {
                    _logger.LogInformation($"{key}: {stopWatch.ElapsedMilliseconds}");
                }
            }
        }

        public async Task<bool> StringFireAndForgetSetAsync(string key, RedisValue value, TimeSpan? timeSpan,
            RedisDataBase redisDataBase = RedisDataBase.Tp)
        {
            key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                return await db.StringSetAsync(key, value, timeSpan, When.Always, CommandFlags.FireAndForget);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return false;
            }
        }

        public bool StringSet(string key, RedisValue value, TimeSpan? timeSpan,
            RedisDataBase redisDataBase = RedisDataBase.Tp)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                return db.StringSet(key, value, timeSpan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return false;
            }
            finally
            {
                stopWatch.Stop();
                if (stopWatch.ElapsedMilliseconds > 2000)
                {
                    _logger.LogInformation($"{key}: {stopWatch.ElapsedMilliseconds}");
                }
            }
        }

        public bool KeyExists(string key, RedisDataBase redisDataBase = RedisDataBase.Tp)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                return db.KeyExists(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return false;
            }
            finally
            {
                stopWatch.Stop();
                if (stopWatch.ElapsedMilliseconds > 2000)
                {
                    _logger.LogInformation($"{key}: {stopWatch.ElapsedMilliseconds}");
                }
            }
        }

        public async Task<TItem> StringGetAsync<TItem>(string key, RedisDataBase redisDataBase = RedisDataBase.Tp)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                var json = await db.StringGetAsync(key);
                return JsonConvert.DeserializeObject<TItem>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return default;
            }
            finally
            {
                stopWatch.Stop();
                if (stopWatch.ElapsedMilliseconds > 2000)
                {
                    _logger.LogInformation($"{key}: {stopWatch.ElapsedMilliseconds}");
                }
            }
        }

        public List<TItem> GetCacheList<TItem>(IEnumerable<string> keys, RedisDataBase redisDataBase = RedisDataBase.Tp)
        {
            try
            {
                var taskList = new List<Task<RedisValue>>();
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                var trans = db.CreateTransaction();
                foreach (var key in keys)
                {
                    var rediskey = $"{redisDataBase}:{RemoveSpecialChars(key)}";
                    var task = db.StringGetAsync(rediskey);
                    taskList.Add(task);
                }

                trans.Execute();

                var reslstList = new List<TItem>();
                foreach (var task in taskList)
                {
                    if (task.Result.HasValue)
                    {
                        var data = JsonConvert.DeserializeObject<TItem>(task.Result);
                        reslstList.Add(data);
                    }
                }
                return reslstList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{keys} | {ex.Message}");
                return default;
            }
        }

        public bool SetCacheList<TItem>(Dictionary<string, TItem> dataList, TimeSpan? timeSpan,
            RedisDataBase redisDataBase = RedisDataBase.Tp)
        {
            try
            {
                var taskList = new List<Task<bool>>();
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                var trans = db.CreateTransaction();
                foreach (var data in dataList)
                {
                    var key = $"{redisDataBase}:{RemoveSpecialChars(data.Key)}";
                    var task = db.StringSetAsync(key, JsonConvert.SerializeObject(data.Value), timeSpan);
                    taskList.Add(task);
                }

                return trans.Execute();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{dataList.Keys} | {ex.Message}");
                return default;
            }
        }

        public bool InsertList<TItem>(string key, TimeSpan? timeSpan, IEnumerable<TItem> list,
            RedisDataBase redisDataBase = RedisDataBase.Tp)
        {
            try
            {
                if (list == null)
                {
                    return false;
                }

                key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
                var taskList = new List<Task<long>>();
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                var trans = db.CreateTransaction();

                foreach (var item in list)
                {
                    var task = db.ListRightPushAsync(key, JsonConvert.SerializeObject(item));
                    taskList.Add(task);
                }

                db.KeyExpire(key, timeSpan);
                return trans.Execute();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return false;
            }
        }

        public List<TItem> GetListByRange<TItem>(string key, long startIndex, long endIndex,
            RedisDataBase redisDataBase = RedisDataBase.Tp)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                var result = new List<TItem>();
                key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                if (!db.KeyExists(key))
                {
                    return result;
                }

                var list = db.ListRange(key, startIndex, endIndex);
                foreach (var item in list)
                {
                    if (item.HasValue)
                    {
                        var data = JsonConvert.DeserializeObject<TItem>(item);
                        result.Add(data);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return default;
            }
            finally
            {
                stopWatch.Stop();
                if (stopWatch.ElapsedMilliseconds > 2000)
                {
                    _logger.LogInformation($"{key}: {stopWatch.ElapsedMilliseconds}");
                }
            }
        }

        public long GetListLength(string key, RedisDataBase redisDataBase = RedisDataBase.Tp)
        {
            try
            {
                key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                return db.ListLength(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return default;
            }
        }

        public async Task<bool> KeyDeleteAsync(string key, RedisDataBase redisDataBase = RedisDataBase.Tp)
        {
            try
            {
                key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                return await db.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return default;
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