using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerV8BLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;
        private readonly ApiV8BLL _apiV8BLL;
        private readonly WagerV8Service _wagerV8Service;
        private readonly ILogger<WagerV8BLL> _logger;
        private readonly DateTimeHelper _dateTimeHelper;

        private readonly int _tpId = (int)Tp.V8;
        private readonly int _timeZone = 0;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";

        public WagerV8BLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiV8BLL = provider.GetRequiredService<ApiV8BLL>();
            _wagerV8Service = provider.GetRequiredService<WagerV8Service>();
            _logger = provider.GetRequiredService<ILogger<WagerV8BLL>>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        }

        public async Task<bool> ExecWagerWorker(int type, int timeRange)
        {
            var wokerTime = GetWorkerTime(type, -180);
            var quick = CheckQuick(wokerTime, 5);
            TimeCast time = _dateTimeHelper.GetTimeByMinute(timeRange, _timeZone, _timeFormat, wokerTime);

            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {

                //// 正式切換後版本
                //var result = await GetWagersAsync(time);
                //if (result)
                //{
                //    _thirdPartyBLL.SetThirdPartyTimer(_tpId, type, time.endUnix);
                //}

                var result = await GetWagersAsync(time);
                if (result)
                {
                    var resultSW = await GetWagersV2Async(time);
                    if (resultSW)
                    {
                        _thirdPartyBLL.SetThirdPartyTimer(_tpId, type, time.endUnix);
                    }
                }
            }

            return quick;
        }

        public async Task<bool> ExecWagerRedoWorker(int type, int timeRange)
        {
            var wokerTime = GetWorkerTime(type, 1);
            var quick = CheckQuick(wokerTime, 120);
            var time = _dateTimeHelper.GetTimeByHour(timeRange, _timeZone, _timeFormat, wokerTime);

            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                //// 正式切換後版本
                //var result = await GetWagersAsync(time, true);
                //if (result)
                //{
                //    _thirdPartyBLL.SetThirdPartyTimer(_tpId, type, time.endUnix);
                //}

                var result = await GetWagersAsync(time, true);
                if (result)
                {
                    var resultSW = await GetWagersV2Async(time, true);
                    if (resultSW)
                    {
                        _thirdPartyBLL.SetThirdPartyTimer(_tpId, type, time.endUnix);
                    }
                }
            }

            return quick;
        }

        //// 正式切換後版本移除
        public async Task<bool> GetWagersAsync(TimeCast wagerTime, bool compare = false)
        {
            var msg = "FATAL_ERROR";
            var workerName = compare ? "GetRedoWagersAsync" : "GetWagersAsync";
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(_tpId);

                // get wager
                var dataWager = await _apiV8BLL.GetBetListByTime(setting["WagerUrl"], setting["Agent"], setting["Deskey"], setting["Md5key"], wagerTime.startUnix, wagerTime.endUnix);

                // error condition
                var errorCode = dataWager.d.code;
                var errorMsg = dataWager.d.code.ToString();

                if (errorCode != 0 && errorCode != 16)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{workerName} {msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return false;
                }

                if (errorCode == 16 || (errorCode == 0 && dataWager.d.count == 0))
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{workerName} {msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return true;
                }

                if (compare)
                {
                    bool check = CompareSummary(wagerTime, dataWager);
                    if (check)
                    {
                        msg = "MATCH";
                        _logger.LogInformation($"{workerName} {msg}");
                        return true;
                    }
                }

                var field = new List<string>();
                for (var i = 0; i < dataWager.d.count; i++)
                {
                    var chairId = (dataWager.d.list.ChairID[i] == null) ? 0 : dataWager.d.list.ChairID[i];
                    var keyValuePairs = new Dictionary<string, dynamic>
                    {
                        { "GameID", dataWager.d.list.GameID[i] },
                        { "Accounts", dataWager.d.list.Accounts[i] },
                        { "ServerID", dataWager.d.list.ServerID[i] },
                        { "KindID", dataWager.d.list.KindID[i] },
                        { "TableID", dataWager.d.list.TableID[i] },
                        { "ChairID", chairId },
                        { "UserCount", dataWager.d.list.UserCount[i] },
                        { "CellScore", dataWager.d.list.CellScore[i] },
                        { "AllBet", dataWager.d.list.AllBet[i] },
                        { "Profit", dataWager.d.list.Profit[i] },
                        { "Revenue", dataWager.d.list.Revenue[i] },
                        { "GameStartTime", dataWager.d.list.GameStartTime[i] },
                        { "GameEndTime", dataWager.d.list.GameEndTime[i] },
                        { "CardValue", dataWager.d.list.CardValue[i] },
                        { "ChannelID", dataWager.d.list.ChannelID[i] },
                        { "LineCode", dataWager.d.list.LineCode[i] }
                    };
                    field.Add(JsonConvert.SerializeObject(keyValuePairs));
                }

                foreach (var item in field)
                {

                    var bet = JsonConvert.DeserializeObject<BetV8>(item);
                    try
                    {
                        _wagerV8Service.InsertWager(bet);
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(WAGERID: {bet.GameID} )";
                        _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                        return false;
                    }
                }

                msg = "FINISH";
                _logger.LogInformation($"{workerName} {msg}\r\n{wagerTime.startTime} ~ {wagerTime.endTime}");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"{workerName} {msg}\r\n{ex.Message}");
                return false;
            }
        }

        //// 正式切換後版本
        //public async Task<bool> GetWagersAsync(TimeCast wagerTime, bool compare = false)
        public async Task<bool> GetWagersV2Async(TimeCast wagerTime, bool compare = false)
        {
            var msg = "FATAL_ERROR";
            var workerName = compare ? "GetRedoWagersV2Async" : "GetWagersV2Async";
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(_tpId);

                // get wager
                var dataWager = await _apiV8BLL.GetBetListByTimeV2(setting, wagerTime.startUnix, wagerTime.endUnix);

                // error condition
                var errorCode = dataWager.d.code;
                var errorMsg = dataWager.d.code.ToString();

                if (errorCode != 0 && errorCode != 16)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{workerName} {msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return false;
                }

                if (errorCode == 16 || (errorCode == 0 && dataWager.d.count == 0))
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{workerName} {msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return true;
                }

                if (compare)
                {
                    bool check = CompareSummary(wagerTime, dataWager);
                    if (check)
                    {
                        msg = "MATCH";
                        _logger.LogInformation($"{workerName} {msg}");
                        return true;
                    }
                }

                var field = new List<string>();
                for (var i = 0; i < dataWager.d.count; i++)
                {
                    var chairId = (dataWager.d.list.ChairID[i] == null) ? 0 : dataWager.d.list.ChairID[i];
                    var keyValuePairs = new Dictionary<string, dynamic>
                    {
                        { "GameID", dataWager.d.list.GameID[i] },
                        { "Accounts", dataWager.d.list.Accounts[i] },
                        { "ServerID", dataWager.d.list.ServerID[i] },
                        { "KindID", dataWager.d.list.KindID[i] },
                        { "TableID", dataWager.d.list.TableID[i] },
                        { "ChairID", chairId },
                        { "UserCount", dataWager.d.list.UserCount[i] },
                        { "CellScore", dataWager.d.list.CellScore[i] },
                        { "AllBet", dataWager.d.list.AllBet[i] },
                        { "Profit", dataWager.d.list.Profit[i] },
                        { "Revenue", dataWager.d.list.Revenue[i] },
                        { "GameStartTime", dataWager.d.list.GameStartTime[i] },
                        { "GameEndTime", dataWager.d.list.GameEndTime[i] },
                        { "CardValue", dataWager.d.list.CardValue[i] },
                        { "ChannelID", dataWager.d.list.ChannelID[i] },
                        { "LineCode", dataWager.d.list.LineCode[i] }
                    };
                    field.Add(JsonConvert.SerializeObject(keyValuePairs));
                }

                foreach (var item in field)
                {

                    var bet = JsonConvert.DeserializeObject<BetV8>(item);
                    try
                    {
                        bet.TpId = (int)Tp.V8;
                        // 統一使用一款執行，後續settleworker再依退水遊戲區分
                        bet.TpGameType = (int)GameType.V8_Poker;
                        _wagerV8Service.InsertWager_V2(bet);

                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(WAGERID: {bet.GameID} )";
                        _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                        return false;
                    }
                }

                msg = "FINISH";
                _logger.LogInformation($"{workerName} {msg}\r\n{wagerTime.startTime} ~ {wagerTime.endTime}");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"{workerName} {msg}\r\n{ex.Message}");
                return false;
            }
        }

        private bool CompareSummary(TimeCast wagerTime, WagerV8Model betDetail)
        {
            // table summary
            var getWagerCount = _wagerV8Service.GetWagerTotal(wagerTime.startUnix, wagerTime.endUnix);
            var wagerCnt = getWagerCount.wagerCount;

            // api summary
            int apiCnt = betDetail.d.count;

            return wagerCnt == apiCnt;
        }

        private DateTime GetWorkerTime(int type, int vary)
        {
            var lastTime = _thirdPartyBLL.GetThirdPartyTimer(_tpId, type);
            return (lastTime == null) ? DateTime.Now.AddHours(-7) : lastTime.StartTime.AddSeconds(vary);
        }

        private bool CheckQuick(DateTime workTime, int excess)
        {
            var diff = DateTime.Now - workTime;
            return DateTime.Now > workTime && diff.TotalMinutes > excess;
        }
    }
}
