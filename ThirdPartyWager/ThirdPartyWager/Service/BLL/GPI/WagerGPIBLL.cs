using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerGPIBLL
    {
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiGPIBLL _apiGPIBLL;
        private readonly CommonBLL _commonBLL;
        private readonly WagerGPIService _wagerGPIService;
        private readonly ILogger<WagerGPIBLL> _logger;

        private readonly int _tpId = (int)Tp.GPI;
        private readonly int _timeZone = 8;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";
        private readonly Dictionary<int, GPIConfig> _siteConfig;

        public class CompareParam
        {
            public decimal Bet { get; set; }
            public decimal Winlose { get; set; }
        }

        public WagerGPIBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiGPIBLL = provider.GetRequiredService<ApiGPIBLL>();
            _wagerGPIService = provider.GetRequiredService<WagerGPIService>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _logger = provider.GetRequiredService<ILogger<WagerGPIBLL>>();
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<GPIConfig>(Tp.GPI.ToString());
        }

        public async Task<bool> ExecWagerWorker(int type, int gameType, int timeRange)
        {
            var workerTime = GetWorkerTime(type, -60, gameType);
            var quick = CheckQuick(workerTime, 5);
            TimeCast time = _dateTimeHelper.GetTimeByMinute(timeRange, _timeZone, _timeFormat, workerTime);

            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                var result = await GetWagersAsync(time, gameType);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimerByGameType(_tpId, type, time.endUnix, gameType);
                }
            }

            return quick;
        }

        public async Task<bool> ExecWagerRedoWorker(int type, int gameType, int timeRange)
        {
            var workerTime = GetWorkerTime(type, 0, gameType);
            var quick = CheckQuick(workerTime, 120);
            var time = _dateTimeHelper.GetTimeByHour(timeRange, _timeZone, _timeFormat, workerTime, 1);

            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                var result = await GetWagersAsync(time, gameType, true);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimerByGameType(_tpId, type, time.endUnix, gameType);
                }
            }

            return quick;
        }

        public async Task<bool> GetWagersAsync(TimeCast wagerTime, int gameType, bool compare = false)
        {
            var msg = "FATAL_ERROR";
            var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
            var wagerData = new List<Items_GPI>();
            var cp = new CompareParam();
            var workerName = compare ? "GetRedoWagersAsync" : "GetWagersAsync";

            try
            {
                foreach (var site in siteList)
                {
                    if (site.TicketKey == 0) continue;

                    // get api info
                    var setting = _siteConfig[site.SiteId];

                    // pagination
                    var page = 1;
                    var totalPage = 1;

                    while (page <= totalPage)
                    {
                        // get wager
                        var dataWager = await _apiGPIBLL.GetBetListByTime(setting, gameType, page, wagerTime.startTime, wagerTime.endTime);

                        // error condition
                        var errorCode = dataWager.resp.error_code;
                        if (errorCode != 0)
                        {
                            msg = "THIRD_PARTY_ERROR";
                            _logger.LogError($"{workerName} {msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                            SystemException systemException = new SystemException($"{msg} - GetWager{_tpId}");
                            throw systemException;
                        }

                        if (errorCode == 0 && dataWager.resp.items.total_row == 0)
                        {
                            msg = "NO_DATA";
                            _logger.LogInformation($"{workerName} {msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                            continue;
                        }

                        totalPage = dataWager.resp.items.total_page;

                        cp.Bet += dataWager.resp.items.item.Sum(x => x.bet);
                        cp.Winlose += dataWager.resp.items.item.Sum(x => x.winlose);

                        wagerData.Add(dataWager.resp.items);

                        page++;

                        // 15 seconds interval between each call
                        Thread.Sleep(17000);
                    }
                }

                if (compare)
                {
                    bool check = await CompareSummaryAsync(wagerTime, gameType, cp);
                    if (check)
                    {
                        msg = "MATCH";
                        _logger.LogInformation($"{workerName} {msg}");
                        return true;
                    }
                }

                foreach (var wagerItem in wagerData)
                {
                    var groups = wagerItem.item.GroupBy(i => (i.round_id));

                    // insert wager to db
                    foreach (var group in groups)
                    {
                        try
                        {
                            var result = _wagerGPIService.InsertWager(group, gameType);
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {group.Key} )";
                            _logger.LogError(ex, $"{workerName} {msg}\r\n{JsonConvert.SerializeObject(group)}\r\n{ex.Message}");
                            return false;
                        }
                    }
                }

                msg = "FINISH";
                _logger.LogInformation($"{workerName} {msg}\r\n{wagerTime.startTime} ~ {wagerTime.endTime}");

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
                _logger.LogError(ex, $"{workerName} {msg}\r\n{ex.Message}");
                return false;
            }
        }

        private async Task<bool> CompareSummaryAsync(TimeCast wagerTime, int gameType, CompareParam cp)
        {
            // table summary
            var spName = (gameType == (int)GameType.GPI_RNG) ? "Finance_TpWagerRng_GPI_Summary_Get" : "Finance_TpWagerLT_GPI_Summary_Get";
            var summary = await _commonBLL.GetWagerSummary(spName, wagerTime.startUnix, wagerTime.endUnix - 1);
            var summaryStake = summary.Sum(x => x.Stake);
            var summaryWinlose = summary.Sum(x => x.WinlostAmount);

            return (cp.Bet == summaryStake && cp.Winlose == summaryWinlose);
        }

        private DateTime GetWorkerTime(int type, int vary, int gameType)
        {
            var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByGameType(_tpId, type, gameType);
            return (lastTime == null) ? DateTime.Now.AddHours(-7) : lastTime.StartTime.AddSeconds(vary);
        }

        private static bool CheckQuick(DateTime workTime, int excess)
        {
            var diff = DateTime.Now - workTime;
            return DateTime.Now > workTime && diff.TotalMinutes > excess;
        }
    }
}
