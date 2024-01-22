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

        public async Task<bool> GetWagersAsync(IEnumerable<HLWagerTrans> unSettleDate,List<DateTime> dayList)
        {
            var msg = "FATAL_ERROR";
            var guid = Guid.NewGuid().ToString();
            try
            {
                //先保留page
                //var page = 1;
                
                //抓單前先試打，若是卡維護就直接false，免得進exception卡時間。
                if (!await TryGetWagersForFirstDay(dayList, guid)) return false;

                var allWagerList = await FetchWagersForDays(dayList, guid);

                //沒單就不跑下面的了
                if (!allWagerList.Any()) 
                    return true;
                
                var distinctWagerData = allWagerList
                    .GroupBy(wager => wager.BetID)
                    .Select(group => group.First())
                    .OrderBy(wager => wager.DrawDate)
                    .ToList();

                var allWagerValid = AllWagerValid(unSettleDate, distinctWagerData);

                ProcessInsertWagers(allWagerValid);
                
                msg = "FINISH";
                _logger.LogInformation($"GetWagersAsync {msg}\r\n{dayList.First():yyyy-MM-dd} ~ {dayList.Last():yyyy-MM-dd}");
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
                
                //結算前一樣試打 要不然一定出事
                var tryGetWinNumber  = await _apiHLBLL.GetWinningNumberByDay(nowDate);
                if (tryGetWinNumber.hl_Response.ErrorCode == 998)
                {
                    msg = "THIRD_PARTY_ERROR_BUSY_TIME";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(tryGetWinNumber)}");
                    return false;
                }
                
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
                        var result = await _apiHLBLL.GetWinningNumberByDay(nowDate);
                        var hlWagerData = result.hl_Response.WagerData;
                        
                        var winMatchedData = from trans in matchedTransWager
                            join data in hlWagerData on trans.TransId equals data.BetID
                            select data; 
                        
                        var insertList = winMatchedData.Select
                            (winWager => CreateHLWagerData(winWager.BetID, winWager.StrikePosition, winWager.SettleAmount)).ToList();

                        var unmatchedTrans = matchedTransWager.Where
                            (trans => !hlWagerData.Any(m => m.BetID == trans.TransId));

                        insertList.AddRange(unmatchedTrans.Select
                            (loseWager => CreateHLWagerData(loseWager.TransId, "NA", "0")));

                        foreach (var item in insertList)
                        {

                            var insertResult = _wagerHLService.UpdateHLWager(item);
                            
                            if (!insertResult)
                            {
                                msg = $"UPDATE_WAGER(WAGERID: {item.TransId} )";
                                _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(item)}");
                            }
                            else
                            {
                                _ = _tpUnsettleService.InsertTpUnsettle(Tp.HaoLong, GameType.HaoLong_4D, item.TransId);
                            }
                        }
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
        #region private

        private async Task<bool> TryGetWagersForFirstDay(IEnumerable<DateTime> dayList, string guid)
        {
            var tryNowDay = dayList.FirstOrDefault().ToString("yyyy-MM-dd");
            var tryGetWager = await _apiHLBLL.GetBetListByTime(tryNowDay, tryNowDay,guid);
            if (tryGetWager.hl_Response.ErrorCode != 998) return true;
            var msg = "THIRD_PARTY_ERROR_BUSY_TIME";
            _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(tryGetWager)}");
            return false;
        }

        private async Task<IEnumerable<HLWagerData>> FetchWagersForDays(IEnumerable<DateTime> dayList, string guid)
        {
            var msg = "FATAL_ERROR";
            var allWagerList = new List<HLWagerData>();
            
            foreach(var day in dayList)
            {
                var nowDay = day.ToString("yyyy-MM-dd");
                var nextDay = day.AddDays(1).ToString("yyyy-MM-dd");

                var wagerData = await _apiHLBLL.GetBetListByTime(nowDay, nextDay,guid);
                    
                var dataWager = wagerData.hl_Response;
                    
                if (dataWager.ErrorCode != 0)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)} , Date:{nowDay} ~ {nextDay}");
                    continue;
                }
                
                if (dataWager.ErrorCode == 3 && dataWager.Desc == "Data NOT Found")
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)} , Date:{nowDay} ~ {nextDay}");
                    continue;
                }
                    
                allWagerList.AddRange(dataWager.WagerData);
            }

            return allWagerList;
        }

        private void ProcessInsertWagers(IEnumerable<HLWagerData> allWagerValid)
        {
            foreach (var wagerData in allWagerValid)
            {
                Console.WriteLine($"TrandID : {wagerData.BetID} , Status : {wagerData.Status}");
                switch (wagerData.Status)
                {
                    // 0:duplicate
                    // 1:void
                    // 2:new
                    case 1:
                        UpdateWager(wagerData.BetID);
                        break;
                    case 2:
                        InsertNewWager(wagerData);
                        break;
                    case 0:
                        break;
                }
            }
        }
        
        private HLSettleInsertData CreateHLWagerData(string betId, string strikePosition, string settleAmount)
        {
            return new HLSettleInsertData()
            {
                TransId = betId,
                StrikePosition = strikePosition,
                SettleAmount = settleAmount,
                SettleFlag = 2
            };
        }
        
        private IEnumerable<HLWagerData> AllWagerValid(IEnumerable<HLWagerTrans> allUnSettleHLWager, IEnumerable<HLWagerData> wagerData)
        {
            var result = new List<HLWagerData>();
            var filteredAndSortedWagers = allUnSettleHLWager
                .Where(wager => wager.SettleFlag != 1)
                //.Where(wager => wager.StrikePosition == "0")
                .OrderBy(wager => wager.drawDate)
                .ToList();
            
            // 情境 1: A 和 B 有符合的項，Status 為 0
            var matched = from a in filteredAndSortedWagers
                join b in wagerData
                    on a.TransId equals b.BetID
                select new HLWagerData
                {
                    BetID = b.BetID,
                    UserName = b.UserName,
                    DateTime = b.DateTime,
                    DrawType = b.DrawType,
                    DrawDate = b.DrawDate,
                    BetNumber = b.BetNumber,
                    BetPosition = b.BetPosition,
                    StrikePosition = b.StrikePosition,
                    BetAmount = b.BetAmount,
                    SettleAmount = b.SettleAmount,
                    Status = 0  // Status 為 0 表示 Duplicate
                };

            // 添加情境 1 的結果
            result.AddRange(matched);
            
            // 情境 3: A 沒有 B 有的項，標記為新注單（Status 設置為 2）
            var unmatchedInA = wagerData
                .Where(b => !filteredAndSortedWagers.Any(a => a.TransId == b.BetID))
                .Select(b => new HLWagerData
                {
                    BetID = b.BetID,
                    UserName = b.UserName,
                    DateTime = b.DateTime,
                    DrawType = b.DrawType,
                    DrawDate = b.DrawDate,
                    BetNumber = b.BetNumber,
                    BetPosition = b.BetPosition,
                    StrikePosition = b.StrikePosition,
                    BetAmount = b.BetAmount,
                    SettleAmount = b.SettleAmount,
                    Status = 2  //Status 為 2 表示 New
                });
            
            result.AddRange(unmatchedInA);

            return result;
        }

        private void InsertNewWager(HLWagerData wagerData)
        {
            _ = _wagerHLService.InsertHLWager(wagerData);
            _ = _tpUnsettleService.InsertTpUnsettle(Tp.HaoLong, GameType.HaoLong_4D, wagerData.BetID);
        }

        private void UpdateWager(string TransId)
        {
            var insert = CreateHLWagerData(TransId, "Void", "0");
            _ = _wagerHLService.UpdateHLWager(insert);
            _ = _tpUnsettleService.InsertTpUnsettle(Tp.HaoLong, GameType.HaoLong_4D, TransId);
        }

        private DateTime ConvertTimeToPlusEight(DateTime dateTime)
        {
            var gmtPlus8 = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, gmtPlus8);
        }
        
        #endregion
    }
}