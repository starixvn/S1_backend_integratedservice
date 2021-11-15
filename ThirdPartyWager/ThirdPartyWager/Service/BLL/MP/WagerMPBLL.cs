using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly Log2File _log2File;
        private readonly ILogger<WagerMPBLL> _logger;

        private readonly int _tpId = (int)Tp.MP;
        private readonly string _logName = Tp.MP.ToString();
        private readonly int _timeZone = 0;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";

        private readonly Dictionary<int, GameType> _gameTypeList = new Dictionary<int, GameType>() {
            {900,GameType.MP_Poker_None },// 900	无抽水
            {910,GameType.MP_Poker_None },// 910	无抽水
            {960,GameType.MP_Poker_None },// 960	无抽水
            {502,GameType.MP_Poker_None },// 502	无抽水
            {180,GameType.MP_Poker },// 180	抽水5%
            {600,GameType.MP_Poker },// 600	抽水5%
            {220,GameType.MP_Poker },// 220	抽水5%
            {620,GameType.MP_Poker },// 620	抽水5%
            {830,GameType.MP_Poker },// 830	抽水5%
            {501,GameType.MP_Poker },// 501	抽水5%
            {504,GameType.MP_Poker },// 504	抽水5%
            {503,GameType.MP_Poker },// 503	抽水5%
            {505,GameType.MP_Poker },// 505	抽水5%
            {506,GameType.MP_Poker },// 506 抽水5%
            {509,GameType.MP_Poker },// 509	抽水5%
            {508,GameType.MP_Poker },// 508	抽水5%
            {510,GameType.MP_Poker_None },// 510	无抽水
            {512,GameType.MP_Poker },// 512	抽水5%
            {514,GameType.MP_Poker_None },// 514	无抽水
            {516,GameType.MP_Poker },// 516	抽水5%
            {1000,GameType.MP_Poker_None },// 1000	无抽水
            {517,GameType.MP_Poker },// 517	抽水5%
            {519,GameType.MP_Poker },// 519	抽水5%
            {515,GameType.MP_Poker_None },// 515	无抽水
        };

        public WagerMPBLL(IServiceProvider provider, IConfiguration configuration)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiMPBLL = provider.GetRequiredService<ApiMPBLL>();
            _wagerMPService = provider.GetRequiredService<WagerMPService>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<WagerMPBLL>>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        }

        public async Task<bool> ExecWagerWorker(int type, int timeRange)
        {
            var wokerTime = GetWorkerTime(type, -180);
            bool quick = CheckQuick(wokerTime, 5);
            var time = _dateTimeHelper.GetTimeByMinute(timeRange, _timeZone, _timeFormat, wokerTime);

            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                bool result = await GetWagersAsync(time.startUnix, time.endUnix);
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
            bool quick = CheckQuick(wokerTime, 120);
            var time = _dateTimeHelper.GetTimeByHour(timeRange, _timeZone, _timeFormat, wokerTime);

            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                // count interval wagers
                var getWagerCount = _wagerMPService.GetWagerTotal(time.startUnix, time.endUnix);
                int total = getWagerCount.wagerCount;

                bool result = await GetWagersAsync(time.startUnix, time.endUnix, true, total);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimer(_tpId, type, time.endUnix);
                }
            }

            return quick;
        }

        private async Task<bool> GetWagersAsync(long startUnix, long endUnix, bool compare = false, int total = 0)
        {
            string msg = "FATAL_ERROR";
            string guid = Guid.NewGuid().ToString();
            LogContext.PushProperty("Tp", _logName);

            try
            {
                var setting = _thirdPartyBLL.GetThirdPartySetting(_tpId);

                // get wager
                var dataWager = await _apiMPBLL.GetBetListByTime(setting["WagerUrl"], setting["Agent"], setting["Deskey"], setting["Md5key"], startUnix, endUnix, guid);

                // error condition
                var errorCode = dataWager.d.code;
                var totalNum = dataWager.d.count;

                if (errorCode != 0 && errorCode != 16)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _log2File.AddResultLog(_logName, guid, msg, JsonConvert.SerializeObject(dataWager));
                    return false;
                }


                if (compare && totalNum == total)
                {
                    msg = "MATCH";
                    _log2File.AddResultLog(_logName, guid, msg, JsonConvert.SerializeObject(dataWager), "Info");
                    return true;
                }

                if (errorCode == 16 || (errorCode == 0 && dataWager.d.count == 0))
                {
                    msg = "NO_DATA";
                    _log2File.AddResultLog(_logName, guid, msg, JsonConvert.SerializeObject(dataWager), "Info");
                    return true;
                }

                var field = new List<string>();
                for (var i = 0; i < dataWager.d.count; i++)
                {
                    Dictionary<string, dynamic> keyValuePairs = new Dictionary<string, dynamic>();
                    keyValuePairs.Add("GameID", dataWager.d.list.GameID[i]);
                    keyValuePairs.Add("Accounts", dataWager.d.list.Accounts[i]);
                    keyValuePairs.Add("ServerID", dataWager.d.list.ServerID[i]);
                    keyValuePairs.Add("KindID", dataWager.d.list.KindID[i]);
                    keyValuePairs.Add("TableID", dataWager.d.list.TableID[i]);
                    keyValuePairs.Add("ChairID", dataWager.d.list.ChairID[i]);
                    keyValuePairs.Add("UserCount", dataWager.d.list.UserCount[i]);
                    keyValuePairs.Add("CellScore", dataWager.d.list.CellScore[i]);
                    keyValuePairs.Add("AllBet", dataWager.d.list.AllBet[i]);
                    keyValuePairs.Add("Profit", dataWager.d.list.Profit[i]);
                    keyValuePairs.Add("Revenue", dataWager.d.list.Revenue[i]);
                    keyValuePairs.Add("GameStartTime", dataWager.d.list.GameStartTime[i]);
                    keyValuePairs.Add("GameEndTime", dataWager.d.list.GameEndTime[i]);
                    keyValuePairs.Add("CardValue", dataWager.d.list.CardValue[i]);
                    keyValuePairs.Add("ChannelID", dataWager.d.list.ChannelID[i]);
                    keyValuePairs.Add("LineCode", dataWager.d.list.LineCode[i]);
                    field.Add(JsonConvert.SerializeObject(keyValuePairs));
                }

                foreach (var item in field)
                {

                    var bet = JsonConvert.DeserializeObject<Bet_MP>(item);
                    try
                    {
                        bet.GameType = _gameTypeList.ContainsKey(bet.KindID) ? (int)_gameTypeList[bet.KindID] : 0;
                        var r = _wagerMPService.InsertWager(bet);
                        _ = r.Result;
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(WAGERID: {bet.GameID} )";
                        _log2File.AddResultLog(_logName, guid, msg, JsonConvert.SerializeObject(bet), "Error", ex.Message);
                        return false;
                    }
                }

                msg = "FINISH";
                _log2File.AddResultLog(_logName, guid, msg, null, "Info");
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
                _log2File.AddResultLog(_logName, guid, msg, null, "Error", ex.Message);
                return false;
            }
        }

        private DateTime GetWorkerTime(int type, int vary)
        {
            var lastTime = _thirdPartyBLL.GetThirdPartyTimer(_tpId, type);
            return (lastTime == null) ? DateTime.Now.AddHours(-6) : lastTime.StartTime.AddSeconds(vary);
        }

        private bool CheckQuick(DateTime workTime, int excess)
        {
            var diff = DateTime.Now - workTime;
            return DateTime.Now > workTime && diff.TotalMinutes > excess;
        }
    }
}
