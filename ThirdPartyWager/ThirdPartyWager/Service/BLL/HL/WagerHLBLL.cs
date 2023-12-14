using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerHLBLL
    {
        private readonly ApiHLBLL _apiHLBLL;
        private readonly WagerHLService _wagerHLService;
        private readonly ILogger<WagerHLBLL> _logger;
        private readonly int _tpId = (int)Tp.HaoLong;
        private readonly TpUnsettleService _tpUnsettleService;
        private readonly WorkerType workerType = WorkerType.MainWager;
        private readonly ThirdPartyBLL _thirdPartyBLL;


        public WagerHLBLL(IServiceProvider provider)
        {
            _apiHLBLL = provider.GetRequiredService<ApiHLBLL>();
            _wagerHLService = provider.GetRequiredService<WagerHLService>();
            _logger = provider.GetRequiredService<ILogger<WagerHLBLL>>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        public async Task<bool> GetWagersAsync(IEnumerable<HLWagerTrans> unSettleDate,string startTime, string endTime)
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
                
                
                
                foreach (var wagerData in dataWager.WagerData)
                {
                    try
                    {
                        if (IsValidWager(unSettleDate, wagerData))
                        {
                            _ =  _wagerHLService.InsertHLWager(wagerData);
                            _ = _tpUnsettleService.InsertTpUnsettle(Tp.HaoLong, GameType.HaoLong_4D, wagerData.BetID);
                        }
                        else
                        {
                            var insert = CreateHLWagerData(wagerData.BetID, "Void", "0");
                            _ =  _wagerHLService.UpdateHLWager(insert);
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


        public async Task<bool> SetSettledWager()
        {
            var msg = "FATAL_ERROR";
            var guid = Guid.NewGuid().ToString();
            try
            {
                var nowDate = DateTime.Now;
                _logger.LogInformation($"HaoLong SetSettledWager Job Time : +0 = {nowDate:yyyy-MM-ddTHH:mm:ss}");
                var getDrawResult = await _apiHLBLL.GetDrawResult(nowDate);
                var ticketKeyHowLong = _thirdPartyBLL.GetAllSiteTicketKey(workerType, _tpId);
                var getHLUnSettledWager = _wagerHLService.GetHLBetUnSettledList(ticketKeyHowLong);

                var uniqueDraw = getDrawResult.hl_Response.DrawResult.Distinct().ToList();

                var updatedDrawDateList = getHLUnSettledWager.Select(trans => new HLWagerTrans
                {
                    TicketKey = trans.TicketKey,
                    SiteId = trans.SiteId,
                    TransId = trans.TransId,
                    TpUserName = trans.TpUserName,
                    drawType = trans.drawType,
                    drawDate = ConvertTimeToPlusEight(trans.drawDate),
                    SettleFlag = trans.SettleFlag
                });
                
                var matchedTransWager = uniqueDraw
                    .Join(updatedDrawDateList,
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
                            var result = await _apiHLBLL.GetWinningNumberByPlayer(item.TpUserName, nowDate);

                            HLWagerData insert;
                            if (result.hl_Response.ErrorCode == 0 && result.hl_Response.WagerData != null)
                            {
                                var settledWager = result.hl_Response.WagerData.FirstOrDefault(x => x.BetID == item.TransId);
                                insert = CreateHLWagerData(settledWager.BetID, settledWager.StrikePosition, settledWager.SettleAmount);
                            }
                            else
                            {
                                insert = CreateHLWagerData(item.TransId, "NA", "0");
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

        private bool IsValidWager(IEnumerable<HLWagerTrans> getHLWager, HLWagerData wagerData)
        {
            return !getHLWager.Any(x => x.TransId != wagerData.BetID);
        }

        private DateTime ConvertTimeToPlusEight(DateTime dateTime)
        {
            var gmtPlus8 = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, gmtPlus8);
        }
    }
}