using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerAPBLL
    {
        public enum GameTypeOfAP
        {
            Slot = 1,
            Mini = 2,
        }

        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiAPBLL _apiAPBLL;
        private readonly WagerAPService _wagerAPService;
        private readonly ILogger<WagerAPBLL> _logger;

        private readonly int _tpId = (int)Tp.AP;
        private readonly int _gameType = (int)GameTypeOfAP.Mini;
        private readonly int _timeZone = 0;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm";
        private readonly Dictionary<int, APConfig> _siteConfigDic;

        public WagerAPBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiAPBLL = provider.GetRequiredService<ApiAPBLL>();
            _wagerAPService = provider.GetRequiredService<WagerAPService>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _logger = provider.GetRequiredService<ILogger<WagerAPBLL>>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<APConfig>(Tp.AP.ToString());
        }

        public async Task<bool> ExecWagerWorker(int siteId, int type, int timeRange)
        {
            var wokerTime = GetWorkerTime(type, -180, siteId);
            var quick = CheckQuick(wokerTime, 5);
            TimeCast time = _dateTimeHelper.GetTimeByMinute(timeRange, _timeZone, _timeFormat, wokerTime);

            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                var result = await GetWagersAsync(time, siteId);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimerByReference(_tpId, type, time.endUnix, _gameType, siteId.ToString());
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
                var result = await GetWagersAsync(time, siteId, true);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimerByReference(_tpId, type, time.endUnix, _gameType, siteId.ToString());
                }
            }

            return quick;
        }

        public async Task<bool> GetWagersAsync(TimeCast wagerTime, int siteId, bool redo = false)
        {
            var workerName = redo ? "GetRedoWagersAsync" : "GetWagersAsync";
            string msg;
            try
            {
                // get api info
                var setting = _siteConfigDic[siteId];

                // pagination
                var page = 1;
                var totalPage = 1;
                while (page <= totalPage)
                {
                    // get wager
                    var dataWager = await _apiAPBLL.GetBetListByTime(setting, wagerTime.startTime, wagerTime.endTime, page);

                    // error condition
                    if (dataWager == null || !string.IsNullOrEmpty(dataWager.errorMesssage))
                    {
                        msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{workerName} {msg}\r\nsiteId={siteId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return false;
                    }

                    var totalNum = dataWager.result.totalCount;
                    totalPage = dataWager.result.totalPage;
                    if (totalNum == 0)
                    {
                        msg = "NO_DATA";
                        _logger.LogInformation($"{workerName} {msg}\r\nsiteId={siteId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return true;
                    }

                    // insert wager to db
                    foreach (var bet in dataWager.result.data)
                    {
                        try
                        {
                            var result = _wagerAPService.InsertWager(bet);
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {bet.transactionReferenceId} )";
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
                _logger.LogError(ex, $"{workerName} {msg}\r\nsiteId={siteId}\r\n{ex.Message}");
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
