using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerPlayTechBLL
    {
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiPlayTechBLL _apiPlayTechBLL;
        private readonly WagerPlayTechService _wagerPlayTechService;
        private readonly ILogger<WagerPlayTechBLL> _logger;

        private readonly int _tpId = (int)Tp.PlayTech;
        private readonly int _timeZone = 0;
        private readonly int _gameType = 0; // 不會區分遊戲，只用來塞timer設定
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:00";

        public WagerPlayTechBLL(IServiceProvider provider)
        {
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiPlayTechBLL = provider.GetRequiredService<ApiPlayTechBLL>();
            _wagerPlayTechService = provider.GetRequiredService<WagerPlayTechService>();
            _logger = provider.GetRequiredService<ILogger<WagerPlayTechBLL>>();
        }

        public async Task<bool> ExecWagerWorker(TpListModel siteData, int type, int timeRange)
        {
            var siteId = siteData.SiteId;
            var wokerTime = GetWorkerTime(type, -120, siteId);
            var quick = CheckQuick(wokerTime, 5);
            TimeCast time = _dateTimeHelper.GetTimeByMinute(timeRange, _timeZone, _timeFormat, wokerTime);

            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                var result = await GetWagersAsync(time, siteData);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimerByReference(_tpId, type, time.endUnix, _gameType, siteId.ToString());
                }
            }

            return quick;
        }

        public async Task<bool> ExecWagerRedoWorker(TpListModel siteData, int type, int timeRange)
        {
            var siteId = siteData.SiteId;
            var wokerTime = GetWorkerTime(type, 1, siteId);
            var quick = CheckQuick(wokerTime, 120);
            var time = _dateTimeHelper.GetTimeByMinute(timeRange, _timeZone, _timeFormat, wokerTime);

            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                var result = await GetWagersAsync(time, siteData, true);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimerByReference(_tpId, type, time.endUnix, 0, siteId.ToString());
                }
            }

            return quick;
        }

        public async Task<bool> GetWagersAsync(TimeCast wagerTime, TpListModel siteData, bool redo = false)
        {
            var workerName = redo ? "GetRedoWagersAsync" : "GetWagersAsync";
            string msg;
            try
            {
                // get api info
                var siteId = siteData.SiteId;
                var setting = siteData.SettingDic;

                // pagination
                var page = 1;
                var totalPage = 1;
                while (page <= totalPage)
                {
                    // get wager
                    var dataWager = await _apiPlayTechBLL.GetBetListByTime(setting, wagerTime.startTime, wagerTime.endTime, page);

                    // error condition
                    if (dataWager == null || dataWager.error != "0")
                    {
                        msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{workerName} {msg}\r\nsiteId={siteId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return false;
                    }

                    if (dataWager.total == 0)
                    {
                        msg = "NO_DATA";
                        _logger.LogInformation($"{msg}\r\nsiteId={siteId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return true;
                    }

                    totalPage = dataWager.pages;

                    // insert wager to db
                    foreach (var bet in dataWager.records)
                    {
                        try
                        {
                            var result = _wagerPlayTechService.InsertWager(bet);
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {bet.roundId} )";
                            _logger.LogError(ex, $"{workerName} {msg}\r\nsiteId={siteId}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                            return false;
                        }
                    }
                    page++;
                }

                msg = "FINISH";
                _logger.LogInformation($"{workerName} {msg}\r\nsiteId={siteId}\r\n{wagerTime.startTime} ~ {wagerTime.endTime}");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"{workerName} {msg}\r\nsiteId={siteData.SiteId}\r\n{ex.Message}");
                return false;
            }
        }

        private DateTime GetWorkerTime(int type, int vary, int siteId)
        {
            var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByReference(_tpId, type, _gameType, siteId.ToString());
            return (lastTime == null) ? DateTime.Now.AddHours(-7) : lastTime.StartTime.AddSeconds(vary);
        }

        private bool CheckQuick(DateTime workTime, int excess)
        {
            var diff = DateTime.Now - workTime;
            return DateTime.Now > workTime && diff.TotalMinutes > excess;
        }
    }
}
