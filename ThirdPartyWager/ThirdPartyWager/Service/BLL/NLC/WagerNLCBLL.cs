using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerNLCBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiNLCBLL _apiNLCBLL;
        private readonly WagerNLCService _wagerNLCService;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly NLCConfig _siteConfig;
        private readonly ILogger<WagerNLCBLL> _logger;

        private readonly int _tpId = (int)Tp.NLC;
        private readonly int _tpGameType = (int)GameType.NLC_RNG;
        private readonly int _timeZone = 0;
        private readonly string _timeFormat = "yyyy-MM-dd'T'HH:mm:ss.fffZ";//2021-09-28T23:59:59.000Z

        public WagerNLCBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiNLCBLL = provider.GetRequiredService<ApiNLCBLL>();
            _wagerNLCService = provider.GetRequiredService<WagerNLCService>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _logger = provider.GetRequiredService<ILogger<WagerNLCBLL>>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<NLCConfig>(Tp.NLC.ToString());
        }

        public async Task<bool> ExecWagerWorker(int type, int timeRange)
        {
            var wokerTime = GetWorkerTime(type, -480);
            var quick = CheckQuick(wokerTime, 5);
            var time = _dateTimeHelper.GetTimeByMinute(timeRange, _timeZone, _timeFormat, wokerTime);

            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                var result = await GetWagersAsync(time.startTime, time.endTime);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimer(_tpId, type, time.endUnix);
                }
            }

            return quick;
        }

        public async Task<bool> ExecWagerRedoWorker(int type, int timeRange)
        {
            var wokerTime = GetWorkerTime(type, 0);
            var quick = CheckQuick(wokerTime, 120);
            var time = _dateTimeHelper.GetTimeByHour(timeRange, _timeZone, _timeFormat, wokerTime, 0);

            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                // count interval wagers
                var getWagerCount = _wagerNLCService.GetWagerRoundInfoTotal(time.startUnix, time.endUnix);
                var wagerTotal = getWagerCount.WagerTotal;
                var payoutTotal = getWagerCount.PayoutTotal;

                var result = await GetWagersAsync(time.startTime, time.endTime, true, wagerTotal, payoutTotal);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimer(_tpId, type, time.endUnix);
                }
            }

            return quick;
        }

        public async Task<bool> ExecUnsettleWorker(int type)
        {
            var result = await GetUnsettleAsync();
            if (result)
            {
                _thirdPartyBLL.SetThirdPartyTimer(_tpId, type, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            }
            return true;
        }

        private async Task<bool> GetWagersAsync(string startTime, string endTime, bool compare = false, decimal wagerTotal = 0, decimal payoutTotal = 0)
        {
            var msg = "FATAL_ERROR";

            try
            {
                var setting = _siteConfig;

                // get wager
                var dataWager = await _apiNLCBLL.GetBetListByTime(setting, startTime, endTime);

                // error condition
                var error = dataWager.Select(x => x.error).FirstOrDefault();

                if (error != null && error != "nodata")
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })}");
                    return false;
                }

                if (compare)
                {
                    var wager = dataWager.Sum(x => x.wager);
                    var payout = dataWager.Sum(x => x.payout);
                    var diffWager = Math.Abs(wager - wagerTotal);
                    var diffPayout = Math.Abs(payout - payoutTotal);
                    if (diffWager < 0.005m && diffPayout < 0.005m)
                    {
                        msg = "MATCH";
                        _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return true;
                    }
                    else
                    {
                        msg = "NO_MATCH";
                        _logger.LogWarning($"{msg}\r\nwager={wager}\r\nwagerTotal={wagerTotal}\r\npayout={payout}\r\npayoutTotal={payoutTotal}");
                    }
                }

                if (error == "nodata")
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return true;
                }

                WriteWager(dataWager);

                msg = "FINISH";
                _logger.LogInformation($"GetWagersAsync {msg}\r\n{startTime} ~ {endTime}");
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

        private async Task<bool> GetUnsettleAsync()
        {
            var msg = "FATAL_ERROR";
            var before = -30;
            try
            {
                var setting = _siteConfig;
                // 取得目前未結算資料
                var unsettledList = _tpUnSettledBLL.GetTpUnsettleList((Tp)_tpId, (GameType)_tpGameType);
                if (unsettledList == null)
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\nunsettledList");
                    return true;
                }

                // 超過30分鐘尚未結算的資料
                var execBefore = DateTimeOffset.UtcNow.AddMinutes(before).ToUnixTimeSeconds();
                var execUnsettleData = unsettledList.Where(i => new DateTimeOffset(i.PartitionTime).ToUnixTimeSeconds() < execBefore).ToList();
                if (execUnsettleData == null)
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\nexecUnsettleData");
                    return true;
                }

                // 排除wager已存在的注單不用重新抓取
                var unsettleOfTransId = execUnsettleData.Select(x => x.TransId).ToList();
                var wagerData = await _wagerNLCService.GetTransIdList(JsonConvert.SerializeObject(unsettleOfTransId));
                var reGetTransId = unsettleOfTransId;
                if (wagerData.Any())
                {
                    var wagerOfTransId = wagerData.Select(x => x.TransId).ToList();
                    reGetTransId = unsettleOfTransId.Except(wagerOfTransId).ToList();
                }

                // 準備以單號取得注單資料
                foreach (var unsettled in execUnsettleData)
                {
                    if (!reGetTransId.Contains(unsettled.TransId))
                    {
                        msg = "DATA_EXIST";
                        _logger.LogInformation($"{msg}\r\nTransId={unsettled.TransId}");
                        continue;
                    }
                    // TooManyRequests:Please try again after 10 seconds
                    Thread.Sleep(12000);

                    // get wager
                    var settleTime = unsettled.PartitionTime.ToString(_timeFormat);
                    var dataWager = await _apiNLCBLL.GetBetListByTransId(setting, unsettled.TransId, settleTime);

                    // error condition
                    var error = dataWager.Select(x => x.error).FirstOrDefault();

                    if (error != null && error != "nodata")
                    {
                        msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\nTransId={unsettled.TransId}\r\n{JsonConvert.SerializeObject(dataWager, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })}");
                        return false;
                    }

                    if (error == "nodata")
                    {
                        msg = "NO_DATA";
                        _logger.LogInformation($"{msg}\r\nTransId={unsettled.TransId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        continue;
                    }

                    WriteWager(dataWager);
                }
                msg = "FINISH";
                _logger.LogInformation($"GetUnsettleAsync {msg}");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"GetUnsettleAsync {msg}\r\n{ex.Message}");
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

        private void WriteWager(List<WagerEVOModel> dataWager)
        {
            var name = "ROUNDID";
            var field = "";
            foreach (var round in dataWager)
            {
                try
                {
                    field = round.id;

                    var roundInfo = new WagerRoundInfo_EVO();
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

                    IList<Participant_NLC> participants = JsonConvert.DeserializeObject<IList<Participant_NLC>>(JsonConvert.SerializeObject(round.participants));
                    foreach (var playerItem in participants)
                    {
                        foreach (var betItem in playerItem.bets)
                        {
                            name = "WAGERID";
                            field = betItem.transactionId;

                            var bet = new Wager_NLC();
                            bet.RoundId = round.id;
                            bet.casinoId = playerItem.casinoId;
                            bet.playerId = playerItem.playerId;
                            bet.playerGameId = playerItem.playerGameId;
                            bet.currency = playerItem.currency;
                            bet.code = betItem.code;
                            bet.stake = betItem.stake;
                            bet.payout = betItem.payout;
                            bet.placedOn = betItem.placedOn;
                            bet.description = betItem.description;
                            bet.transactionId = betItem.transactionId;
                            bet.playMode = playerItem.playMode;
                            bet.mathId = playerItem.mathId;
                            bet.channel = !string.IsNullOrEmpty(playerItem.channel) ? playerItem.channel : "";
                            bet.os = !string.IsNullOrEmpty(playerItem.os) ? playerItem.os : "";
                            bet.device = !string.IsNullOrEmpty(playerItem.device) ? playerItem.device : "";
                            bet.skinId = playerItem.skinId;
                            bet.brandId = playerItem.brandId;
                            bet.currencyRateVersion = playerItem.currencyRateVersion;
                            bet.participants = JsonConvert.SerializeObject(playerItem);
                            var r = _wagerNLCService.InsertWager(bet);
                            _ = r.Result;
                        }
                    }
                    var rr = _wagerNLCService.InsertWagerRoundInfo(roundInfo);
                    _ = rr.Result;
                }
                catch (Exception ex)
                {
                    var msg = $"INSERT_WAGER({name}: {field} )";
                    _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(round)}\r\n{ex.Message}");
                    throw;
                }
            }
        }
    }
}
