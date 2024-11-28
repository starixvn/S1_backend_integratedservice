using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.KY;
using ThirdPartyWager.Service.Model.enums;
using Newtonsoft.Json;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerKYBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiKYBLL _apiKYBLL;
        private readonly WagerKYService _wagerKYService;
        private readonly ILogger<WagerKYBLL> _logger;
        private readonly DateTimeHelper _dateTimeHelper;

        private readonly int _tpId = (int)Tp.KY;
        private readonly int _timeZone = 0;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";
        private readonly int _gameType = 0; // 不會區分遊戲，只用來塞timer設定

        public WagerKYBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiKYBLL = provider.GetRequiredService<ApiKYBLL>();
            _wagerKYService = provider.GetRequiredService<WagerKYService>();
            _logger = provider.GetRequiredService<ILogger<WagerKYBLL>>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        }

        public async Task<bool> ExecWagerWorker(KYConfig config, int type, int timeRange, string tpCurrencyId, int siteId)
        {
            // TODO: what is the meaning of -180?
            var workerTime = GetWorkerTime(type, -180, $"{siteId.ToString()}-{tpCurrencyId}");
            var quick = CheckQuick(workerTime, 5);
            TimeCast time = _dateTimeHelper.GetTimeByMinute(timeRange, _timeZone, _timeFormat, workerTime);
            
            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                var result = await GetWagersAsync(config, time, tpCurrencyId);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimerByReference(_tpId, type, time.endUnix, _gameType, $"{siteId.ToString()}-{tpCurrencyId}");
                }
            }

            return quick;
        }

        public async Task<bool> GetWagersAsync(KYConfig config, TimeCast wagerTime, string tpCurrencyId, bool compare = false)
        {
            var msg = "FATAL_ERROR";
            // TODO: what does compare mean? to redo or 1st time?
            var workerName = compare ? "GetRedoWagersAsync" : "GetWagersAsync";
            try
            {
                var agent = Convert.ToInt32(config.CurrencyData[tpCurrencyId].Agent);
                
                var dataWager = await _apiKYBLL.GetBetListByTime(config, wagerTime.startUnix, wagerTime.endUnix, tpCurrencyId);

                var errorCode = dataWager.d.code;
                var errorMsg = dataWager.d.code.ToString();

                if (errorCode != 0 && errorCode != 16)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{workerName} {msg} {tpCurrencyId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return false;
                }

                if (errorCode == 16 || (errorCode == 0 && dataWager.d.count == 0))
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{workerName} {msg} {tpCurrencyId}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return true;
                }
                if (compare)
                {
                    bool check = CompareSummary(wagerTime, dataWager, agent);
                    if (check)
                    {
                        msg = "MATCH";
                        _logger.LogInformation($"{workerName} {msg} {tpCurrencyId}");
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
                        { "LineCode", dataWager.d.list.LineCode[i] },
                        { "Currency", dataWager.d.list.Currency[i] },
                        { "Language", dataWager.d.list.Language[i] }
                    };
                    field.Add(JsonConvert.SerializeObject(keyValuePairs));
                }

                foreach (var item in field)
                {
                    var bet = JsonConvert.DeserializeObject<BetKY>(item);
                    try
                    {
                        bet.TpId = (int)Tp.KY;
                        // 統一使用一款執行，後續settleworker再依退水遊戲區分
                        bet.TpGameType = (int)GameType.KY_Poker;
                        _wagerKYService.InsertWager(bet);
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
                _logger.LogError(ex, $"{workerName} {msg} {tpCurrencyId}\r\n{ex.Message}");
                return false;
            }
        }

        private bool CompareSummary(TimeCast wagerTime, WagerKYModel betDetail, int agent)
        {
            var getWagerCount = _wagerKYService.GetWagerTotal(wagerTime.startUnix, wagerTime.endUnix, agent);
            var wagerCnt = getWagerCount.wagerCount;
            int apiCnt = betDetail.d.count;

            return wagerCnt == apiCnt;
        }

        private DateTime GetWorkerTime(int type, int vary, string reference)
        {
            var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByReference(_tpId, type, _gameType, reference);
            return (lastTime == null) ? DateTime.Now.AddHours(-7) : lastTime.StartTime.AddSeconds(vary);
        }

        private bool CheckQuick(DateTime workTime, int excess)
        {
            var diff = DateTime.Now - workTime;
            return DateTime.Now > workTime && diff.TotalMinutes > excess;
        }
    }
}
