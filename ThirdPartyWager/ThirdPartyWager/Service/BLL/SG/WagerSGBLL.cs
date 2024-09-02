using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerSGBLL
    {
        private readonly ApiSGBLL _apiSGBLL;
        private readonly WagerSGService _wagerSGService;
        private readonly ILogger<WagerSGBLL> _logger;
        private readonly TpUnsettleService _tpUnsettleService;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly Dictionary<int, SGConfig> _siteConfigDic;
        private readonly int _tpId = (int)Tp.SG;
        private readonly string _tpName = Tp.SG.ToString();
        private readonly int _timeZone = 8;
        private readonly string _timeFormat = "yyyyMMdd'T'HHmmss";

        public WagerSGBLL(IServiceProvider provider)
        {
            _apiSGBLL = provider.GetRequiredService<ApiSGBLL>();
            _wagerSGService = provider.GetRequiredService<WagerSGService>();
            _logger = provider.GetRequiredService<ILogger<WagerSGBLL>>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<SGConfig>(_tpName);
        }

        public async Task<bool> ExecWagerWorker(int siteId, int type, int timeRange)
        {
            var wokerTime = GetWorkerTime(type, -1, siteId);
            var quick = CheckQuick(wokerTime, 5);
            TimeCast time = _dateTimeHelper.GetTimeByMinute(timeRange, _timeZone, _timeFormat, wokerTime);

            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                var result = await GetWagersV2Async(time, siteId);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimerByReference(_tpId, type, time.endUnix, 0, siteId.ToString());
                }
            }

            return quick;
        }

        public async Task<bool> GetWagersV2Async(TimeCast wagerTime, int siteId)
        {
            var msg = "FATAL_ERROR";
            var guid = Guid.NewGuid().ToString();
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
                    var dataWager = await _apiSGBLL.GetBetListByTime(setting, wagerTime.startTime, wagerTime.endTime, page, guid);

                    // error condition
                    var errorCode = dataWager.code;

                    if (errorCode != 0)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\nSiteId={siteId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return false;
                    }

                    var totalNum = dataWager.resultCount;
                    totalPage = dataWager.pageCount;
                    if (totalNum == 0)
                    {
                        msg = "NO_DATA";
                        _logger.LogInformation($"{msg}\r\nSiteId={siteId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return true;
                    }

                    // insert wager to db
                    foreach (var bet in dataWager.list)
                    {
                        try
                        {
                            var result = 0;
                            if (!string.IsNullOrEmpty(bet.categoryId) && bet.categoryId.Equals("BN", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            var gameType = GetGameType(bet.gameCode);
                            result = gameType is GameType.SG_RNG or GameType.SG_Table ? _wagerSGService.InsertWagerRng(bet) : _wagerSGService.InsertWager(bet);

                            if (result > 0)
                            {
                                _tpUnsettleService.InsertTpUnsettle(Tp.SG, gameType, bet.referenceId);
                            }
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {bet.ticketId} )";
                            _logger.LogError(ex, $"{msg}\r\nSiteId={siteId}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                            return false;
                        }
                    }
                    page++;
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

        //public async Task<bool> GetWagersAsync(int tpId, string startTime, string endTime)
        //{
        //    var msg = "FATAL_ERROR";
        //    var guid = Guid.NewGuid().ToString();
        //    try
        //    {
        //        // get api info
        //        var setting = _siteConfig;

        //        // pagination
        //        var page = 1;
        //        var totalPage = 1;
        //        while (page <= totalPage)
        //        {
        //            // get wager
        //            var dataWager = await _apiSGBLL.GetBetListByTime(_siteConfig, startTime, endTime, page, guid);

        //            // error condition
        //            var errorCode = dataWager.code;

        //            if (errorCode != 0)
        //            {
        //                msg = "THIRD_PARTY_ERROR";
        //                _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
        //                return false;
        //            }

        //            var totalNum = dataWager.resultCount;
        //            totalPage = dataWager.pageCount;
        //            if (totalNum == 0)
        //            {
        //                msg = "NO_DATA";
        //                _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
        //                return true;
        //            }

        //            // insert wager to db
        //            foreach (var bet in dataWager.list)
        //            {
        //                try
        //                {
        //                    var result = 0;
        //                    if (!string.IsNullOrEmpty(bet.categoryId) && bet.categoryId.Equals("BN", StringComparison.OrdinalIgnoreCase))
        //                    {
        //                        continue;
        //                    }

        //                    var gameType = GetGameType(bet.gameCode);
        //                    result = gameType is GameType.SG_RNG or GameType.SG_Table ? _wagerSGService.InsertWagerRng(bet) : _wagerSGService.InsertWager(bet);

        //                    if (result > 0)
        //                    {
        //                        _tpUnsettleService.InsertTpUnsettle(Tp.SG, gameType, bet.referenceId);
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    msg = $"INSERT_WAGER(WAGERID: {bet.ticketId} )";
        //                    _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
        //                    return false;
        //                }
        //            }
        //            page++;
        //        }

        //        msg = "FINISH";
        //        _logger.LogInformation($"GetWagersAsync {msg}\r\n{startTime} ~ {endTime}");
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        msg = "FATAL_ERROR";
        //        _logger.LogError(ex, $"GetWagersAsync {msg}\r\n{ex.Message}");
        //        return false;
        //    }

        //}

        private static GameType GetGameType(string gameCode)
        {
            if (gameCode.StartsWith("F-", StringComparison.OrdinalIgnoreCase))
            {
                return GameType.SG_Fish;
            }

            if (gameCode.StartsWith("C-", StringComparison.OrdinalIgnoreCase))
            {
                return GameType.SG_Table;
            }

            return GameType.SG_RNG;
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
