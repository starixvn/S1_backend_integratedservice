using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerWMBLL
    {
        private readonly ApiWMBLL _apiWMBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;
        private readonly WagerWMService _wagerWMService;
        private readonly ILogger<WagerWMBLL> _logger;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly IHostEnvironment _environment;
        private readonly TpUnsettleService _tpUnsettleService;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly Dictionary<int, WMConfig> _siteConfigDic;
        private readonly int _tpId = (int)Tp.WM;
        private readonly string _tpName = Tp.WM.ToString();
        private readonly int _timeZone = 8;
        private readonly string _timeFormat = "yyyyMMddHHmmss";

        public WagerWMBLL(IServiceProvider provider, IHostEnvironment environment)
        {
            _apiWMBLL = provider.GetRequiredService<ApiWMBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _wagerWMService = provider.GetRequiredService<WagerWMService>();
            _logger = provider.GetRequiredService<ILogger<WagerWMBLL>>();
            _tpCurrency = provider.GetRequiredService<CommonBLL>().GetTpCurrencyList(_tpId);
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _environment = environment;
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<WMConfig>(_tpName);
        }

        public async Task<bool> ExecWagerWorker(int siteId, int type, int timeRange)
        {
            var wokerTime = GetWorkerTime(type, 0, siteId);
            var quick = CheckQuick(wokerTime, 5);
            TimeCast time = _dateTimeHelper.GetTimeByMinute(timeRange, _timeZone, _timeFormat, wokerTime);

            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                var result = await GetWagersAsync(time, siteId);
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
            TimeCast time = _dateTimeHelper.GetTimeByHour(timeRange, _timeZone, _timeFormat, wokerTime);

            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                var result = await RedoWagersAsync(time, siteId);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimerByReference(_tpId, type, time.endUnix, 0, siteId.ToString());
                }
            }
            return quick;
        }

        public async Task<bool> GetWagersAsync(TimeCast wagerTime, int siteId)
        {
            string msg;
            try
            {
                // get api info
                var setting = _siteConfigDic[siteId];

                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
                var serverList = _commonBLL.GetSiteList();
                var currencyList = JsonConvert.DeserializeObject<IEnumerable<int>>(serverList.First(x => x.SiteId == siteId).CurrencyList);

                foreach (var currency in _tpCurrency.Where(x => currencyList.Contains(x.CurrencyId)))
                {
                    // DEV Nova只有RMB可使用
                    if (!_environment.IsProduction())
                    {
                        if (siteId == 1 && currency.TpCurrencyId != "RMB")
                        {
                            continue;
                        }
                    }

                    // get wager
                    var dataWager = await _apiWMBLL.GetBetListByTime(setting, currency.TpCurrencyId, wagerTime.startTime, wagerTime.endTime);

                    // error condition
                    var errorCode = dataWager.errorCode;

                    if (errorCode == 107)
                    {
                        msg = "NO_DATA";
                        _logger.LogInformation($"{msg}\r\nSiteId={siteId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        continue;
                    }

                    if (errorCode != 0)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\nSiteId={siteId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return false;
                    }

                    foreach (var bet in dataWager.result)
                    {
                        try
                        {
                            bet.Currency = currency.TpCurrencyId;
                            var result = _wagerWMService.InsertWager(bet);
                            if (result > 0)
                            {
                                _tpUnsettleService.InsertTpUnsettle(Tp.WM, GameType.WM_LiveCasino, bet.TransId);
                            }
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {bet.betId} )";
                            _logger.LogError(ex, $"{msg}\r\nSiteId={siteId}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                            return false;
                        }
                    }
                }

                msg = "FINISH";
                _logger.LogInformation($"GetWagersAsync {msg}\r\nSiteId={siteId}\r\n{wagerTime.startTime} ~ {wagerTime.endTime}");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"GetWagersAsync {msg}\r\nSiteId={siteId}\r\n{ex.Message}");
                return false;
            }
        }

        public async Task<bool> RedoWagersAsync(TimeCast wagerTime, int siteId)
        {
            string msg;
            try
            {
                var reCheck = false;

                // get api info
                var setting = _siteConfigDic[siteId];
                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
                var serverList = _commonBLL.GetSiteList();
                var currencyList = JsonConvert.DeserializeObject<IEnumerable<int>>(serverList.First(x => x.SiteId == siteId).CurrencyList);

                foreach (var currency in _tpCurrency.Where(x => currencyList.Contains(x.CurrencyId)))
                {
                    // DEV Nova只有RMB可使用
                    if (!_environment.IsProduction())
                    {
                        if (siteId == 1 && currency.TpCurrencyId != "RMB")
                        {
                            continue;
                        }
                    }

                    // count interval wagers
                    var getWagerCount = _wagerWMService.GetWagerTotal(wagerTime.startUnix, wagerTime.endUnix, currency.TpCurrencyId, siteId);
                    var wagerCount = getWagerCount.wagerCount;

                    var dataWager = await _apiWMBLL.GetBetListByTime(setting, currency.TpCurrencyId, wagerTime.startTime, wagerTime.endTime);

                    // error condition
                    var errorCode = dataWager.errorCode;
                    if (errorCode == 107)
                    {
                        msg = "NO_DATA";
                        _logger.LogInformation($"{msg}\r\nSiteId={siteId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        continue;
                    }

                    if (errorCode != 0)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\nSiteId={siteId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return false;
                    }

                    // db wager count equals api bet count do nothing
                    var totalNum = dataWager.result.Count();
                    if (totalNum == wagerCount)
                    {
                        msg = "MATCH";
                        _logger.LogInformation($"{msg}\r\nSiteId={siteId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        continue;
                    }

                    // insert wager to db
                    foreach (var bet in dataWager.result)
                    {
                        try
                        {
                            bet.Currency = currency.TpCurrencyId;
                            var result = _wagerWMService.InsertWager(bet);
                            if (result > 0)
                            {
                                _tpUnsettleService.InsertTpUnsettle(Tp.WM, GameType.WM_LiveCasino, bet.TransId);
                            }
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {bet.betId} )";
                            _logger.LogError(ex, $"{msg}\r\nSiteId={siteId}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                            return false;
                        }
                    }

                    reCheck = true;
                }

                msg = "FINISH";
                _logger.LogInformation($"RedoWagersAsync {msg}\r\nSiteId={siteId}\r\n{wagerTime.startTime} ~ {wagerTime.endTime}");
                return !(reCheck);
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"RedoWagersAsync {msg}\r\nSiteId={siteId}\r\n{ex.Message}");
                return false;
            }
        }

        private DateTime GetWorkerTime(int type, int vary, int siteId)
        {
            var initTime = DateTime.UtcNow;
            var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByReference(_tpId, type, 0, siteId.ToString());
            if (lastTime == null)
            {
                return DateTime.Now.AddHours(-3);
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
