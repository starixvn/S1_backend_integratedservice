using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerSimplePlayBLL
    {
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiSimplePlayBLL _apiSimplePlayBLL;
        private readonly CommonBLL _commonBLL;
        private readonly WagerSimplePlayService _wagerSimplePlayService;
        private readonly ILogger<WagerSimplePlayBLL> _logger;
        private readonly Dictionary<int, SimplePlayConfig> _siteConfigDic;
        private readonly int _tpId = (int)Tp.SimplePlay;
        private readonly string _tpName = Tp.SimplePlay.ToString();
        private readonly int _timeZone = 8;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";

        public WagerSimplePlayBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiSimplePlayBLL = provider.GetRequiredService<ApiSimplePlayBLL>();
            _wagerSimplePlayService = provider.GetRequiredService<WagerSimplePlayService>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _logger = provider.GetRequiredService<ILogger<WagerSimplePlayBLL>>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<SimplePlayConfig>(_tpName);
        }

        public async Task<bool> ExecWagerWorker(int siteId, int type, int timeRange)
        {
            var wokerTime = GetWorkerTime(type, -180, siteId);
            var quick = CheckQuick(wokerTime, 5);
            TimeCast time = _dateTimeHelper.GetTimeByMinute(timeRange, _timeZone, _timeFormat, wokerTime);

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
            var wokerTime = GetWorkerTime(type, 1, siteId);
            var quick = CheckQuick(wokerTime, 120);
            var time = _dateTimeHelper.GetTimeByHour(timeRange, _timeZone, _timeFormat, wokerTime);

            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                var result = await GetWagersAsync(siteId, time, true);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimerByReference(_tpId, type, time.endUnix, 0, siteId.ToString());
                }
            }

            return quick;
        }

        public async Task<bool> GetWagersAsync(int siteId, TimeCast wagerTime, bool compare = false)
        {
            var msg = "FATAL_ERROR";
            var workerName = compare ? "GetRedoWagersAsync" : "GetWagersAsync";
            try
            {
                // get wager
                var dataWager = await _apiSimplePlayBLL.GetBetListByTime(siteId, wagerTime.startTime, wagerTime.endTime);

                // error condition
                var errorCode = dataWager.GetAllBetDetailsForTimeIntervalResponse?.ErrorMsgId ?? dataWager.APIResponse.ErrorMsgId;
                if (errorCode != 0)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{workerName} {msg},siteId={siteId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return false;
                }

                if (errorCode == 0 && dataWager.GetAllBetDetailsForTimeIntervalResponse.BetDetailList == null)
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{workerName} {msg},siteId={siteId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return true;
                }

                if (compare)
                {
                    bool check = await CompareSummaryAsync(siteId, wagerTime, dataWager.GetAllBetDetailsForTimeIntervalResponse.BetDetailList.BetDetail);
                    if (check)
                    {
                        msg = "MATCH";
                        _logger.LogInformation($"{workerName} {msg},siteId={siteId}");
                        return true;
                    }
                }

                var groups = dataWager.GetAllBetDetailsForTimeIntervalResponse.BetDetailList.BetDetail.GroupBy(i => (i.GameType, i.Username, i.GameID));
                var gameTypeList = new List<string> { "slot"/*, "multiplayer"*/ };

                // insert wager to db
                foreach (var group in groups)
                {
                    var transId = $"{group.Key.GameID}_{group.Key.Username}";

                    try
                    {
                        var gameType = group.Key.GameType;
                        if (!gameTypeList.Contains(gameType.ToLower()))
                        {
                            msg = $"INSERT_WAGER(GameType: {group.Key.GameType} )";
                            _logger.LogError($"{workerName} {msg},siteId={siteId}\r\n{JsonConvert.SerializeObject(group)}");
                            return false;
                        }

                        var result = _wagerSimplePlayService.InsertWager(group);
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(WAGERID: {transId} )";
                        _logger.LogError(ex, $"{workerName} {msg},siteId={siteId}\r\n{JsonConvert.SerializeObject(group)}\r\n{ex.Message}");
                        return false;
                    }
                }

                msg = "FINISH";
                _logger.LogInformation($"{workerName} {msg},siteId={siteId}\r\n{wagerTime.startTime} ~ {wagerTime.endTime}");

                if (compare)
                {
                    // after rewrite go check next time
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"{workerName} {msg},siteId={siteId}\r\n{ex.Message}");
                return false;
            }
        }

        private async Task<bool> CompareSummaryAsync(int siteId, TimeCast wagerTime, IList<Bet_SimplePlay> betDetail)
        {
            var config = _siteConfigDic[siteId];
            var siteIds = JsonConvert.SerializeObject(config.CommonSiteIds);

            // table summary
            var summarySlot = await _commonBLL.GetWagerSummaryBySiteIds("Service_TpWagerRng_SimplePlay_Total_Get", wagerTime.startUnix, wagerTime.endUnix, siteIds);
            var summaryStake = summarySlot.Sum(x => x.Stake);
            var summaryWinlose = summarySlot.Sum(x => x.WinlostAmount);

            // api summary
            var stake = betDetail.Sum(x => x.BetAmount);
            var winlose = betDetail.Sum(x => x.ResultAmount);

            return stake == summaryStake && winlose == summaryWinlose;
        }

        private DateTime GetWorkerTime(int type, int vary, int siteId)
        {
            var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByReference(_tpId, type, 0, siteId.ToString());
            return (lastTime == null) ? DateTime.Now.AddHours(-2) : lastTime.StartTime.AddSeconds(vary);
        }

        private static bool CheckQuick(DateTime workTime, int excess)
        {
            var diff = DateTime.Now - workTime;
            return DateTime.Now > workTime && diff.TotalMinutes > excess;
        }
    }
}
