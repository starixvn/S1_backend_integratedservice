using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerIconicBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiIconicBLL _apiIconicBLL;
        private readonly WagerIconicService _wagerIconicService;
        private readonly ILogger<WagerIconicBLL> _logger;

        public WagerIconicBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiIconicBLL = provider.GetRequiredService<ApiIconicBLL>();
            _wagerIconicService = provider.GetRequiredService<WagerIconicService>();
            _logger = provider.GetRequiredService<ILogger<WagerIconicBLL>>();
        }

        public async Task<bool> GetWagersV2Async(int tpId, long startUnix, long endUnix, string currency)
        {
            var msg = "FATAL_ERROR";
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

                var perPage = 1000;
                var page = 1;
                var totalPage = 1;
                while (page <= totalPage)
                {
                    // get wager
                    var dataWager = await _apiIconicBLL.GetBetListByTimeV2(setting["ApiUrl"], setting[$"token_{currency}"], startUnix, endUnix, page, perPage);

                    // error condition
                    if (dataWager.error != null)
                    {
                        var errorCode = dataWager.error.status;
                        if (errorCode != 0)
                        {
                            msg = "THIRD_PARTY_ERROR";
                            _logger.LogError($"{msg}\r\n{currency}\r\n{JsonConvert.SerializeObject(dataWager)}");
                            return false;
                        }
                    }

                    if (dataWager.totalSize == 0)
                    {
                        msg = "NO_DATA";
                        _logger.LogInformation($"{msg}\r\n{currency}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return true;
                    }

                    totalPage = (int)((dataWager.totalSize / perPage) + 1);
                    foreach (var bet in dataWager.data)
                    {
                        try
                        {
                            if (bet.status == "playing")
                            {
                                continue;
                            }

                            if (bet.gameType == "fish")
                            {
                                _wagerIconicService.InsertWagerFish(bet);
                            }
                            else
                            {
                                _wagerIconicService.InsertWagerSlot(bet);
                            }

                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {bet.id} )";
                            _logger.LogError(ex, $"{msg}\r\n{currency}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                            return false;
                        }
                    }
                    page++;
                }

                msg = "FINISH";
                _logger.LogInformation($"GetWagersV2Async {msg}\r\n{currency}\r\n{startUnix} ~ {endUnix}");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"GetWagersV2Async {msg}\r\n{currency}\r\n{ex.Message}");
                return false;
            }
        }

        public async Task<bool> RedoWagersAsync(int tpId, string startTime, string endTime, long startUnix, long endUnix, string currency)
        {
            var msg = "FATAL_ERROR";
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

                // db wagers summary
                var getDBWagerSummary = GetDBWagerSummary(startUnix, endUnix, currency);

                var getWagerSummary = await _apiIconicBLL.GetBetSummaryByTime(setting["ApiUrl"], setting[$"token_{currency}"], startUnix, endUnix);

                // error condition
                if (getWagerSummary.error != null)
                {
                    var errorCode = getWagerSummary.error.status;
                    if (errorCode != 0)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\n{currency}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                        return false;
                    }
                }

                if (getWagerSummary.data.Count == 0)
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    return true;
                }


                if (getDBWagerSummary.bet == getWagerSummary.data[0].bet && getDBWagerSummary.win == getWagerSummary.data[0].win)
                {
                    msg = "MATCH";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    return true;
                }

                // parse 10 minute
                var st = startUnix;
                var ed = startUnix + 600;
                for (var i = 1; i <= 6; i++)
                {
                    if (i > 1)
                    {
                        st += 600;
                        ed += 600;
                    }
                    _ = await GetWagersV2Async(tpId, st, ed, currency);
                }

                msg = "FINISH_REDO";
                _logger.LogInformation($"RedoWagersAsync {msg}\r\n{currency}\r\n{startUnix} ~ {endUnix}");
                return false;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"RedoWagersAsync {msg}\r\n{currency}\r\n{ex.Message}");
                return false;
            }
        }

        private TpWagerSummary_AT GetDBWagerSummary(long startUnix, long endUnix, string currency)
        {
            var wagerCurrency = (currency == "VND" || currency == "IDR") ? $"{currency}1000" : currency;
            var getSummaryFish = _wagerIconicService.GetWagerFishTotal(startUnix, endUnix, wagerCurrency);
            var getSummarySlot = _wagerIconicService.GetWagerSlotTotal(startUnix, endUnix, wagerCurrency);

            return new TpWagerSummary_AT()
            {
                wagerCount = getSummaryFish.wagerCount + getSummarySlot.wagerCount,
                bet = getSummaryFish.bet + getSummarySlot.bet,
                validBet = getSummaryFish.validBet + getSummarySlot.validBet,
                win = getSummaryFish.win + getSummarySlot.win,
            };
        }
    }
}
