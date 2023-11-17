using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL.HL
{
    public class WagerHLBLL
    {
        private readonly ApiHLBLL _apiHLBLL;
        private readonly WagerHLService _wagerHLService;
        private readonly ILogger<WagerHLBLL> _logger;
        private readonly int _tpId = (int)Tp.HaoLong;
        private readonly HLConfig _siteHLConfig;

        public WagerHLBLL(IServiceProvider provider)
        {
            _apiHLBLL = provider.GetRequiredService<ApiHLBLL>();
            _wagerHLService = provider.GetRequiredService<WagerHLService>();
            _logger = provider.GetRequiredService<ILogger<WagerHLBLL>>();
            _siteHLConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<HLConfig>(Tp.Allbet.ToString());
        }

        public async Task<bool> GetWagersAsync(string startTime, string endTime)
        {
            var msg = "FATAL_ERROR";
            var guid = Guid.NewGuid().ToString();
            try
            {
                //先保留page
                //var page = 1;

                var result = await _apiHLBLL.GetBetListByTime(startTime,endTime,guid);
                var dataWager = result.hl_Response;

                if (dataWager.ErrorCode != 0 && dataWager.ErrorCode != 998)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return false;
                }
                
                if (dataWager.ErrorCode == 3 && dataWager.Desc == "Data NOT Found")
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return true;
                }

                //繁忙時間 就直接回傳了
                if (dataWager.ErrorCode == 998)
                {
                    msg = "Busy Time";
                    _logger.LogInformation($"HaoLong GetWagersAsync {msg}\r\n{startTime} ~ {endTime}");
                    return true;
                }
                
                var getHLWager = _wagerHLService.GetHLBetUnSettledList();
                
                foreach (var wagerData in dataWager.WagerData)
                {
                    try
                    {
                        if (IsValidWager(getHLWager, wagerData))
                        {
                            _ =  _wagerHLService.InsertHLWager(wagerData);
                        }
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(WAGERID: {wagerData.BetID} )";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(wagerData)}\r\n{ex.Message}");
                        return false;
                    }
                }
                
                msg = "FINISH";
                _logger.LogInformation($"GetWagersAsync {msg}\r\n{startTime} ~ {endTime}");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"GetWagersAsync {msg}\r\n{ex.Message}");
                return false;
            }
        }


        public async Task<bool> SetSettledWager(string startTime, string endTime)
        {
            var msg = "FATAL_ERROR";
            var guid = Guid.NewGuid().ToString();
            try
            {
                var nowDate = DateTime.Now;
                _logger.LogInformation($"HaoLong Job Time : {nowDate}");
                var getDrawResult = await _apiHLBLL.GetDrawResult(nowDate);
                var getHLUnSettledWager = _wagerHLService.GetHLBetUnSettledList();

                var uniqueDraw = getDrawResult.hl_Response.DrawResult.Distinct().ToList();
                
                var matchedTransWager = uniqueDraw
                    .Join(getHLUnSettledWager,
                        extracted => new { extracted.drawType, extracted.drawDate },
                        hlBet => new { hlBet.drawType, hlBet.drawDate },
                        (extracted, hlBet) => hlBet)
                    .ToList();
                
                
                if (matchedTransWager.Any())
                {
                    try 
                    {
                        var tasks = matchedTransWager.Select(async item =>
                        {
                            var result = await _apiHLBLL.GetWinningNumberByPlayer(item.loginID, nowDate);

                            HLWagerData insert;
                            if (result.hl_Response.ErrorCode == 0 && result.hl_Response.WagerData != null)
                            {
                                var settledWager = result.hl_Response.WagerData.FirstOrDefault(x => x.BetID == item.betID);
                                insert = CreateHLWagerData(settledWager.BetID, settledWager.StrikePosition, settledWager.SettleAmount);
                            }
                            else
                            {
                                insert = CreateHLWagerData(item.betID, "NA", "0");
                            }

                            var insertResult = await _wagerHLService.UpdateHLWager(insert);
                            if (!insertResult)
                            {
                                msg = $"UPDATE_WAGER(WAGERID: {insert.BetID} )";
                                _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(insert)}");
                            }
                        });

                        await Task.WhenAll(tasks);

                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(WAGERID: {matchedTransWager.Count()} )";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(matchedTransWager)}\r\n{ex.Message}");
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"SetSettledWager {msg}\r\n{ex.Message}");
                return false;
            }
        }
        
        private HLWagerData CreateHLWagerData(string betId, string strikePosition, string settleAmount)
        {
            return new HLWagerData()
            {
                BetID = betId,
                StrikePosition = strikePosition,
                SettleAmount = settleAmount,
                SettleFlag = 2
            };
        }
        
        private bool IsValidWager(IEnumerable<HLBetTrans> getHLWager, HLWagerData wagerData)
        {
            return getHLWager.Any(x => x.betID != wagerData.BetID);
        }
    }
}