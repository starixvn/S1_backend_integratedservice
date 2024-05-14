using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerMPBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiMPBLL _apiMPBLL;
        private readonly WagerMPService _wagerMPService;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly TpUnsettleService _tpUnsettleService;
        private readonly ILogger<WagerMPBLL> _logger;
        private readonly MPConfig _siteConfig;

        private readonly int _tpId = (int)Tp.MP;
        private readonly string _tpName = Tp.MP.ToString();
        private readonly int _timeZone = 0;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";

        private readonly Dictionary<int, GameType> _gameTypeList = new()
        {
            { 900, GameType.MP_Poker_None },// 900	无抽水
            { 910, GameType.MP_Poker_None },// 910	无抽水
            { 960, GameType.MP_Poker_None },// 960	无抽水
            { 502, GameType.MP_Poker_None },// 502	无抽水
            { 180, GameType.MP_Poker },// 180	抽水5%
            { 600, GameType.MP_Poker },// 600	抽水5%
            { 220, GameType.MP_Poker },// 220	抽水5%
            { 620, GameType.MP_Poker },// 620	抽水5%
            { 830, GameType.MP_Poker },// 830	抽水5%
            { 501, GameType.MP_Poker },// 501	抽水5%
            { 504, GameType.MP_Poker },// 504	抽水5%
            { 503, GameType.MP_Poker },// 503	抽水5%
            { 505, GameType.MP_Poker },// 505	抽水5%
            { 506, GameType.MP_Poker },// 506 抽水5%
            { 509, GameType.MP_Poker },// 509	抽水5%
            { 508, GameType.MP_Poker },// 508	抽水5%
            { 510, GameType.MP_Poker_None },// 510	无抽水
            { 512, GameType.MP_Poker },// 512	抽水5%
            { 514, GameType.MP_Poker_None },// 514	无抽水
            { 516, GameType.MP_Poker },// 516	抽水5%
            { 1000, GameType.MP_Poker_None },// 1000	无抽水
            { 517, GameType.MP_Poker },// 517	抽水5%
            { 519, GameType.MP_Poker },// 519	抽水5%
            { 515, GameType.MP_Poker_None },// 515	无抽水
            { 181, GameType.MP_Poker },// 181	抽水5%
            { 182, GameType.MP_Poker },// 182	抽水5%
        };

        public WagerMPBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiMPBLL = provider.GetRequiredService<ApiMPBLL>();
            _wagerMPService = provider.GetRequiredService<WagerMPService>();
            _logger = provider.GetRequiredService<ILogger<WagerMPBLL>>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<MPConfig>(_tpName);
        }

        public async Task<bool> ExecWagerWorker(int type, int timeRange)
        {
            var wokerTime = GetWorkerTime(type, -180);
            var quick = CheckQuick(wokerTime, 5);
            var time = _dateTimeHelper.GetTimeByMinute(timeRange, _timeZone, _timeFormat, wokerTime);

            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                var result = await GetWagersAsync(time.startUnix, time.endUnix);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimer(_tpId, type, time.endUnix);
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
                // count interval wagers
                var getWagerCount = _wagerMPService.GetWagerTotal(time.startUnix, time.endUnix);
                var total = getWagerCount.wagerCount;

                var result = await GetWagersAsync(time.startUnix, time.endUnix, true, total);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimer(_tpId, type, time.endUnix);
                }
            }

            return quick;
        }

        private async Task<bool> GetWagersAsync(long startUnix, long endUnix, bool compare = false, int total = 0)
        {
            string msg;
            try
            {
                var setting = _siteConfig;

                // get wager
                var dataWager = await _apiMPBLL.GetBetListByTime(setting, startUnix, endUnix);

                // error condition
                var errorCode = dataWager.d.code;
                var totalNum = dataWager.d.count;

                if (errorCode != 0 && errorCode != 16)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return false;
                }


                if (compare && totalNum == total)
                {
                    msg = "MATCH";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return true;
                }

                if (errorCode == 16 || (errorCode == 0 && dataWager.d.count == 0))
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return true;
                }

                var field = new List<string>();
                for (var i = 0; i < dataWager.d.count; i++)
                {
                    var keyValuePairs = new Dictionary<string, dynamic>
                    {
                        { "GameID", dataWager.d.list.GameID[i] },
                        { "Accounts", dataWager.d.list.Accounts[i] },
                        { "ServerID", dataWager.d.list.ServerID[i] },
                        { "KindID", dataWager.d.list.KindID[i] },
                        { "TableID", dataWager.d.list.TableID[i] },
                        { "ChairID", dataWager.d.list.ChairID[i] },
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

                    var bet = JsonConvert.DeserializeObject<Bet_MP>(item);
                    try
                    {
                        bet.GameType = _gameTypeList.ContainsKey(bet.KindID) ? (int)_gameTypeList[bet.KindID] : 0;
                        var result = await _wagerMPService.InsertWager(bet);

                        if (result > 0)
                        {
                            _tpUnsettleService.InsertTpUnsettle(Tp.MP, (GameType)bet.GameType, bet.GameID);
                        }
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(WAGERID: {bet.GameID} )";
                        _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                        return false;
                    }
                }

                msg = "FINISH";
                _logger.LogInformation($"GetWagersAsync {msg}\r\n{startUnix} ~ {endUnix}");
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
                _logger.LogError(ex, $"GetWagersAsync {msg}\r\n{ex.Message}");
                return false;
            }
        }

        private DateTime GetWorkerTime(int type, int vary)
        {
            var lastTime = _thirdPartyBLL.GetThirdPartyTimer(_tpId, type);
            return (lastTime == null) ? DateTime.Now.AddHours(-6) : lastTime.StartTime.AddSeconds(vary);
        }

        private static bool CheckQuick(DateTime workTime, int excess)
        {
            var diff = DateTime.Now - workTime;
            return DateTime.Now > workTime && diff.TotalMinutes > excess;
        }
    }
}
