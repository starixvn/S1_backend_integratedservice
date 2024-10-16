using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerAGBLL
    {
        private readonly ILogger<WagerAGBLL> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiAGBLL _apiAGBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly WagerAGService _wagerAGService;
        private readonly Dictionary<int, AGConfig> _siteConfigDic;
        private readonly int _tpId = (int)Tp.AG;
        private readonly string _tpName = Tp.AG.ToString();
        private readonly int _timeZone = -4;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";

        public WagerAGBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerAGBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiAGBLL = provider.GetRequiredService<ApiAGBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _wagerAGService = provider.GetRequiredService<WagerAGService>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<AGConfig>(_tpName);
        }

        public async Task<bool> ExecWagerWorker(int siteId, int type, int timeRange)
        {
            if (_siteConfigDic.TryGetValue(siteId, out var config))
            {
                if (string.IsNullOrEmpty(config.ApiUrl))
                {
                    _logger.LogError($"siteId={siteId}, siteConfigDic config={JsonConvert.SerializeObject(config)}");
                    return false;
                }
            }

            var wokerTime = GetWorkerTime(type, 0, siteId);
            var quick = CheckQuick(wokerTime, 5);
            var time = _dateTimeHelper.GetTimeByMinute(timeRange - 1, _timeZone, _timeFormat, wokerTime);

            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                var result = await GetWagersAsync(siteId, config, time);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimerByReference(_tpId, type, time.endUnix, 0, siteId.ToString());
                }
            }

            return quick;
        }

        public async Task<bool> ExecWagerRedoWorker(int siteId, int type, int timeRange)
        {
            if (_siteConfigDic.TryGetValue(siteId, out var config))
            {
                if (string.IsNullOrEmpty(config.ApiUrl))
                {
                    _logger.LogError($"siteId={siteId}, siteConfigDic config={JsonConvert.SerializeObject(config)}");
                    return false;
                }
            }

            var wokerTime = GetWorkerTime(type, 1, siteId);
            var quick = CheckQuick(wokerTime, 120);
            var time = _dateTimeHelper.GetTimeByHour(timeRange, _timeZone, _timeFormat, wokerTime);

            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                var result = await RedoWagersAsync(siteId, config, time);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimerByReference(_tpId, type, time.endUnix, 0, siteId.ToString());
                }
            }

            return quick;
        }

        public async Task<bool> GetWagersAsync(int siteId, AGConfig config, TimeCast wagerTime)
        {
            string msg;
            try
            {
                var wagerUrl = config.WagerUrl;
                var hashKey = config.HashKey;
                var data_cagent = config.data_cagent;

                // pagination
                var page = 1;
                var totalPage = 1;
                while (page <= totalPage)
                {
                    // get wager
                    var dataWager = await _apiAGBLL.GetBetListByTime(wagerUrl, hashKey, data_cagent, wagerTime.startTime, wagerTime.endTime, page);

                    // error condition
                    var errorCode = dataWager.result.info;

                    if (errorCode != 0)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"GetWagersAsync {msg}\r\n{siteId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return false;
                    }

                    var totalNum = dataWager.result.addition.total;
                    totalPage = dataWager.result.addition.totalpage;
                    if (totalNum == 0)
                    {
                        msg = "NO_DATA";
                        _logger.LogInformation($"GetWagersAsync {msg}\r\n{siteId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return true;
                    }

                    foreach (var bet in dataWager.result.row)
                    {
                        try
                        {
                            _wagerAGService.InsertWager(bet);
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {bet.billNo} )";
                            _logger.LogError($"GetWagersAsync {msg}\r\n{siteId}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                            return false;
                        }
                    }
                    page++;
                }

                msg = "FINISH";
                _logger.LogInformation($"GetWagersAsync {msg}\r\n{siteId}\r\n{wagerTime.startTime} ~ {wagerTime.endTime}");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"GetWagersAsync {msg}\r\n{siteId}\r\n{ex.Message}");
                return false;
            }
        }

        public async Task<bool> RedoWagersAsync(int siteId, AGConfig config, TimeCast wagerTime)
        {
            string msg;
            try
            {
                var wagerUrl = config.WagerUrl;
                var hashKey = config.HashKey;
                var data_cagent = config.data_cagent;

                // count interval wagers
                var getWagerCount = _wagerAGService.GetWagerTotal(wagerTime.startUnix, wagerTime.endUnix);
                var wagerCount = DealWagerTotalToMainSiteId(getWagerCount).FirstOrDefault(i => i.MainSiteId == siteId).WagerCount;

                var getWagerSummary = await _apiAGBLL.GetBetSummaryByTime(wagerUrl, hashKey, data_cagent, wagerTime.startTime, wagerTime.endTime);
                var arrSummary = getWagerSummary.result;

                // error condition
                var errorCode = arrSummary.info;
                if (errorCode != 0)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"RedoWagersAsync {msg}\r\n{siteId}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    return false;
                }

                if (errorCode == 0 && arrSummary.row == null)
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"RedoWagersAsync {msg}\r\n{siteId}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    return true;
                }

                // db wager count equals api bet count do nothing
                var totalNum = 0;
                foreach (var data in arrSummary.row)
                {
                    totalNum += data.wagersTotal;
                }
                if (totalNum == wagerCount)
                {
                    msg = "MATCH";
                    _logger.LogInformation($"RedoWagersAsync {msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    return true;
                }

                // parse 10 minute
                var st = Convert.ToDateTime(wagerTime.startTime);
                var ed = Convert.ToDateTime(wagerTime.startTime).AddMinutes(10).AddMilliseconds(-1);
                var bg = "";
                var fn = "";
                for (var i = 1; i <= 6; i++)
                {
                    var page = 1;
                    var totalPage = 1;
                    if (i > 1)
                    {
                        st = st.AddMinutes(10);
                        ed = ed.AddMinutes(10);
                    }
                    bg = st.ToString("yyyy-MM-dd HH:mm:ss");
                    fn = ed.ToString("yyyy-MM-dd HH:mm:ss");
                    while (page <= totalPage)
                    {
                        var dataWager = await _apiAGBLL.GetBetListByTime(wagerUrl, hashKey, data_cagent, bg, fn, page);
                        var arrWager = dataWager.result;

                        // error condition
                        errorCode = arrWager.info;
                        if (errorCode != 0)
                        {
                            msg = "THIRD_PARTY_ERROR";
                            _logger.LogError($"RedoWagersAsync {msg}\r\n{siteId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                            return false;
                        }

                        // pagination
                        var pageInfo = arrWager.addition;
                        var totalNo = pageInfo.total;
                        totalPage = pageInfo.totalpage;
                        if (totalNo == 0)
                        {
                            msg = "NO_DATA";
                            _logger.LogInformation($"RedoWagersAsync {msg}\r\n{siteId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                            continue;
                        }

                        foreach (var bet in arrWager.row)
                        {
                            try
                            {
                                _wagerAGService.InsertWager(bet);
                            }
                            catch (Exception ex)
                            {
                                msg = $"INSERT_WAGER(WAGERID: {bet.billNo} )";
                                _logger.LogError(ex, $"RedoWagersAsync {msg}\r\n{siteId}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                                return false;
                            }
                        }
                        page++;
                    }
                }

                msg = "FINISH_REDO";
                _logger.LogInformation($"RedoWagersAsync {msg}\r\n{siteId}\r\n{wagerTime.startTime} ~ {wagerTime.endTime}");
                return false;

            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"RedoWagersAsync {msg}\r\n{siteId}\r\n{ex.Message}");
                return false;
            }
        }

        private static IEnumerable<CurrencyStatistics_AG> DealWagerTotalToMainSiteId(IEnumerable<WagerAGTotal> wagerCountData)
        {
            return wagerCountData.GroupBy(data => data.MainSiteId).Select(
                g => new CurrencyStatistics_AG
                {
                    MainSiteId = g.Key,
                    WagerCount = g.Sum(y => y.WagerCount)
                }
            ).ToList();
        }

        private DateTime GetWorkerTime(int type, int vary, int siteId)
        {
            var initTime = DateTime.UtcNow;
            var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByReference(_tpId, type, 0, siteId.ToString());
            if (lastTime == null)
            {
                return initTime.AddDays(-1);
            }
            else
            {
                return lastTime.StartTime.AddSeconds(vary);
            }
        }

        private static bool CheckQuick(DateTime workTime, int excess)
        {
            var diff = DateTime.Now - workTime;
            return DateTime.Now > workTime && diff.TotalMinutes >= excess;
        }
    }
}
