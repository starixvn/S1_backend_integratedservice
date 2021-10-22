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
    public class WagerEVOBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiEVOBLL _apiEVOBLL;
        private readonly WagerEVOService _wagerEVOService;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly Log2File _log2File;
        private readonly ILogger<WagerEVOBLL> _logger;

        private readonly int _tpId = (int)Tp.EVO;
        private readonly string _logName = Tp.EVO.ToString();
        private readonly int _timeZone = 0;
        private readonly string _timeFormat = "yyyy-MM-dd'T'HH:mm:ss.fffZ";//2021-09-28T23:59:59.000Z

        public WagerEVOBLL(IServiceProvider provider, IConfiguration configuration)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiEVOBLL = provider.GetRequiredService<ApiEVOBLL>();
            _wagerEVOService = provider.GetRequiredService<WagerEVOService>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<WagerEVOBLL>>();
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
                bool result = await GetWagersAsync(time.startTime, time.endTime);
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
                var getWagerCount = _wagerEVOService.GetWagerRoundInfoTotal(time.startUnix, time.endUnix);
                decimal wagerTotal = getWagerCount.WagerTotal;
                decimal payoutTotal = getWagerCount.PayoutTotal;

                bool result = await GetWagersAsync(time.startTime, time.endTime, true, wagerTotal, payoutTotal);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimer(_tpId, type, time.endUnix);
                }
            }

            return quick;
        }

        private async Task<bool> GetWagersAsync(string startTime, string endTime, bool compare = false, decimal wagerTotal = 0, decimal payoutTotal = 0)
        {
            string msg = "FATAL_ERROR";
            string guid = Guid.NewGuid().ToString();
            LogContext.PushProperty("Tp", _logName);

            try
            {
                var setting = _thirdPartyBLL.GetThirdPartySetting(_tpId);

                // get wager
                var dataWager = await _apiEVOBLL.GetBetListByTime(setting["ApiUrl"], setting["CasinoKey"], setting["gameHistoryApiToken"], setting["GameProvider"], startTime, endTime, guid);

                // error condition
                var error = dataWager.Select(x => x.error).FirstOrDefault();

                if (error != null && error != "nodata")
                {
                    msg = "THIRD_PARTY_ERROR";
                    _log2File.AddResultLog(_logName, guid, msg, JsonConvert.SerializeObject(dataWager, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }));
                    return false;
                }

                if (compare)
                {
                    decimal wager = dataWager.Sum(x => x.wager);
                    decimal payout = dataWager.Sum(x => x.payout);

                    if (wager == wagerTotal && payout == payoutTotal)
                    {
                        msg = "MATCH";
                        _log2File.AddResultLog(_logName, guid, msg, JsonConvert.SerializeObject(dataWager), "Info");
                        return true;
                    }
                }

                if (error == "nodata")
                {
                    msg = "NO_DATA";
                    _log2File.AddResultLog(_logName, guid, msg, JsonConvert.SerializeObject(dataWager), "Info");
                    return true;
                }

                string name = "ROUNDID";
                string field = "";
                foreach (var round in dataWager)
                {
                    try
                    {
                        field = round.id;

                        WagerRoundInfo_EVO roundInfo = new WagerRoundInfo_EVO();
                        roundInfo.id = round.id;
                        roundInfo.gameProvider = round.gameProvider;
                        roundInfo.startedAt = round.startedAt;
                        roundInfo.settledAt = round.settledAt;
                        roundInfo.status = round.status;
                        roundInfo.gameType = round.gameType;
                        roundInfo.table = JsonConvert.SerializeObject(round.table);
                        roundInfo.dealer = JsonConvert.SerializeObject(round.dealer);
                        roundInfo.currency = round.currency;
                        roundInfo.result = JsonConvert.SerializeObject(round.result);
                        roundInfo.wager = round.wager;
                        roundInfo.payout = round.payout;
                        roundInfo.TableId = round.table["id"];

                        IList<Participant_EVO> participants = JsonConvert.DeserializeObject<IList<Participant_EVO>>(JsonConvert.SerializeObject(round.participants));
                        foreach (var playerItem in participants)
                        {
                            foreach (var betItem in playerItem.bets)
                            {
                                name = "WAGERID";
                                field = betItem.transactionId;

                                Wager_EVO bet = new Wager_EVO();
                                bet.RoundId = round.id;
                                bet.casinoId = playerItem.casinoId;
                                bet.playerId = playerItem.playerId;
                                //bet.screenName = playerItem.screenName;
                                //bet.sessionId = playerItem.sessionId;
                                //bet.casinoSessionId = playerItem.casinoSessionId;
                                bet.currency = playerItem.currency;
                                bet.code = betItem.code;
                                bet.stake = betItem.stake;
                                bet.payout = betItem.payout;
                                bet.placedOn = betItem.placedOn;
                                bet.description = betItem.description;
                                bet.transactionId = betItem.transactionId;
                                //bet.configOverlays = JsonConvert.SerializeObject(playerItem.configOverlays);
                                bet.playMode = playerItem.playMode;
                                bet.channel = playerItem.channel;
                                //bet.os = playerItem.os;
                                //bet.device = playerItem.device;
                                bet.participants = JsonConvert.SerializeObject(playerItem);
                                var r = _wagerEVOService.InsertWager(bet);
                                _ = r.Result;
                            }
                        }
                        var rr = _wagerEVOService.InsertWagerRoundInfo(roundInfo);
                        _ = rr.Result;
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER({name}: {field} )";
                        _log2File.AddResultLog(_logName, guid, msg, JsonConvert.SerializeObject(round), "Error", ex.Message);
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
            return (lastTime == null) ? DateTime.Now.AddHours(-7) : lastTime.StartTime.AddSeconds(vary);
        }

        private bool CheckQuick(DateTime workTime, int excess)
        {
            var diff = DateTime.Now - workTime;
            return DateTime.Now > workTime && diff.TotalMinutes > excess;
        }
    }
}
