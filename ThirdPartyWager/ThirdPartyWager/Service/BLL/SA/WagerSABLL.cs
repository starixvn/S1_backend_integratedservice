using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerSABLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiSABLL _apiSABLL;
        private readonly WagerSAService _wagerSAService;
        private readonly ILogger<WagerSABLL> _logger;
        private readonly MySqlClient _wagerDbClient;
        private readonly TpUnsettleService _tpUnsettleService;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly Dictionary<int, SAConfig> _siteConfigDic;
        private readonly int _tpId = (int)Tp.SA;
        private readonly string _tpName = Tp.SA.ToString();
        private readonly int _timeZone = 8;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";

        public WagerSABLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiSABLL = provider.GetRequiredService<ApiSABLL>();
            _wagerSAService = provider.GetRequiredService<WagerSAService>();
            _logger = provider.GetRequiredService<ILogger<WagerSABLL>>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<SAConfig>(_tpName);
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
                var result = await GetWagersAsync(siteId, time);
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
                var result = await RedoWagersAsync(siteId, time);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimerByReference(_tpId, type, time.endUnix, 0, siteId.ToString());
                }
            }

            return quick;
        }

        public async Task<bool> GetWagersAsync(int siteId, TimeCast wagerTime)
        {
            var msg = "FATAL_ERROR";

            try
            {
                var setting = _siteConfigDic[siteId];
                if (setting.WagerUrl == null)
                {
                    return false;
                }

                // get wager
                var dataWager = await _apiSABLL.GetBetListByTime(setting, wagerTime.startTime, wagerTime.endTime);

                // error condition
                var errorCode = dataWager.GetAllBetDetailsForTimeIntervalResponse?.ErrorMsgId ?? dataWager.APIResponse.ErrorMsgId;
                if (errorCode != 0)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"GetWagersAsync {msg}\r\n{siteId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return false;
                }

                if (errorCode == 0 && dataWager.GetAllBetDetailsForTimeIntervalResponse.BetDetailList == null)
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"GetWagersAsync {msg}\r\n{siteId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return true;
                }

                var groups = dataWager.GetAllBetDetailsForTimeIntervalResponse.BetDetailList.BetDetail.GroupBy(i =>
                    (i.Username, i.GameID));

                // insert wager to db
                foreach (var group in groups)
                {
                    await using var conn = _wagerDbClient.GetMySqlConnection();
                    conn.Open();
                    await using var trans = await conn.BeginTransactionAsync();

                    try
                    {
                        foreach (var bet in group)
                        {
                            bet.GameResult = JsonConvert.SerializeObject(bet.GameResult);
                        }

                        var result = _wagerSAService.InsertWager(group, trans);
                        if (result == group.Count())
                        {
                            trans.Commit();
                            _tpUnsettleService.InsertTpUnsettle(Tp.SA, GameType.SA_LiveCasino,
                                $"{group.Key.GameID}_{group.Key.Username}");
                        }
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(WAGERID: {group.Key.GameID}_{group.Key.Username} )";
                        _logger.LogError(ex, $"GetWagersAsync {msg}\r\n{siteId}\r\n{JsonConvert.SerializeObject(group)}\r\n{ex.Message}");
                        return false;
                    }

                    await conn.CloseAsync();
                    await conn.DisposeAsync();
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

        public async Task<bool> RedoWagersAsync(int siteId, TimeCast wagerTime)
        {
            var msg = "FATAL_ERROR";

            try
            {
                var setting = _siteConfigDic[siteId];
                if (setting.WagerUrl == null)
                {
                    return false;
                }

                // count interval wagers
                var getWagerCount = _wagerSAService.GetWagerTotal(wagerTime.startUnix, wagerTime.endUnix);
                var wagerCount = DealWagerTotalToMainSiteId(getWagerCount, siteId);

                var getWagerSummary = await _apiSABLL.GetBetSummaryByTime(setting, wagerTime.startTime, wagerTime.endTime);

                // error condition
                var errorCode = getWagerSummary.GetTransactionDetailsResponse?.ErrorMsgId ?? getWagerSummary.APIResponse.ErrorMsgId;
                if (errorCode != 0)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"RedoWagersAsync {msg}\r\n{siteId}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    return false;
                }
                if (errorCode == 0 && getWagerSummary.GetTransactionDetailsResponse.Details == null)
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"RedoWagersAsync {msg}\r\n{siteId}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    return true;
                }

                // db wager count equals api bet count do nothing
                var totalNum = 0;
                foreach (var data in getWagerSummary.GetTransactionDetailsResponse.Details.TransactionDetails)
                {
                    totalNum += data.Count;
                }
                if (totalNum == wagerCount)
                {
                    msg = "MATCH";
                    _logger.LogInformation($"RedoWagersAsync {msg}\r\n{siteId}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    return true;
                }

                var dataWager = await _apiSABLL.GetBetListByTime(setting, wagerTime.startTime, wagerTime.endTime);

                // error condition
                errorCode = dataWager.GetAllBetDetailsForTimeIntervalResponse?.ErrorMsgId ?? dataWager.APIResponse.ErrorMsgId;
                if (errorCode != 0)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"RedoWagersAsync {msg}\r\n{siteId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return false;
                }
                if (errorCode == 0 && dataWager.GetAllBetDetailsForTimeIntervalResponse.BetDetailList == null)
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"RedoWagersAsync {msg}\r\n{siteId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return true;
                }

                var groups = dataWager.GetAllBetDetailsForTimeIntervalResponse.BetDetailList.BetDetail.GroupBy(i =>
                    (i.Username, i.GameID));

                // insert wager to db
                foreach (var group in groups)
                {
                    await using var conn = _wagerDbClient.GetMySqlConnection();
                    conn.Open();
                    await using var trans = await conn.BeginTransactionAsync();
                    try
                    {
                        foreach (var bet in group)
                        {
                            bet.GameResult = JsonConvert.SerializeObject(bet.GameResult);
                        }

                        var result = _wagerSAService.InsertWager(group, trans);
                        if (result == group.Count())
                        {
                            trans.Commit();
                            _tpUnsettleService.InsertTpUnsettle(Tp.SA, GameType.SA_LiveCasino,
                                $"{group.Key.GameID}_{group.Key.Username}");
                        }
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(WAGERID: {group.Key.GameID}_{group.Key.Username} )";
                        _logger.LogError(ex, $"RedoWagersAsync {msg}\r\n{siteId}\r\n{JsonConvert.SerializeObject(group)}\r\n{ex.Message}");
                        return false;
                    }

                    await conn.CloseAsync();
                    await conn.DisposeAsync();
                }

                msg = "FINISH";
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

        private static int DealWagerTotalToMainSiteId(IEnumerable<WagerSATotal> wagerCountData, int siteId)
        {
            var data = wagerCountData.GroupBy(data => data.MainSiteId).Select(
                g => new CurrencyStatistics_SA
                {
                    MainSiteId = g.Key,
                    WagerCount = g.Sum(y => y.WagerCount)
                }
            ).ToList();
            var mainSiteData = data.FirstOrDefault(i => i.MainSiteId == siteId);
            return (mainSiteData == null) ? 0 : mainSiteData.WagerCount;
        }

        private DateTime GetWorkerTime(int type, int vary, int siteId)
        {
            var initTime = DateTime.UtcNow;
            var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByReference(_tpId, type, 0, siteId.ToString());
            if (lastTime == null)
            {
                return DateTime.Now.AddMinutes(-30);
            }
            else
            {
                return (lastTime.StartTime >= initTime) ? lastTime.StartTime.AddMinutes(-3) : lastTime.StartTime.AddSeconds(vary);
            }
        }

        private static bool CheckQuick(DateTime workTime, int excess)
        {
            var diff = DateTime.Now - workTime;
            return DateTime.Now > workTime && diff.TotalMinutes > excess;
        }
    }
}
