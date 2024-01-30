using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;


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
                
                var lastWagerDate = unSettleDate.MaxBy(x => x.dateTime).dateTime;
                var allWagerValid =  distinctWagerData
                    .Where(wager => ConvertTimeToUTCTime(wager.DateTime) > lastWagerDate)
                    .OrderBy(wager => wager.DrawDate)
                    .ToList();
                
                foreach (var wagerData in allWagerValid)
                { 
                    InsertNewWager(wagerData);
                }
                
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

        public async Task<bool> TryGetSettledResult()
        {
            var msg = "FATAL_ERROR";
            try
            {
                var nowDate = DateTime.Now;
                //結算前一樣試打 要不然一定出事
                var tryGetWinNumber  = await _apiHLBLL.GetWinningNumberByDay(nowDate);
                if (tryGetWinNumber.hl_Response.ErrorCode == 998)
                {
                    msg = "THIRD_PARTY_ERROR_BUSY_TIME_BY_SETTLED";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(tryGetWinNumber)}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"SetSettledWager {msg}\r\n{ex.Message}");
                return false;
            }

            return true;
        }


        public async Task<bool> SetSettledWager(IEnumerable<HLWagerTrans> unSettleDate)
        {
            var msg = "FATAL_ERROR";
            var guid = Guid.NewGuid().ToString();
            try
            {
                var nowDate = DateTime.Now;
                _logger.LogInformation($"HaoLong SetSettledWager Job Time : +0 = {nowDate:yyyy-MM-ddTHH:mm:ss}");
                
                var uniqueDrawDates = unSettleDate
                    .Where(wager => wager.SettleFlag == 0)
                    .Select(wager => ConvertDateTime(wager.drawDate).Date)
                    .Distinct() 
                    .ToList();
                
                var updatedDrawDateList = unSettleDate
                    .Where(x => x.SettleFlag == 0)
                    .Select(trans => new HLWagerTrans
                    {
                        TicketKey = trans.TicketKey,
                        SiteId = trans.SiteId,
                        TransId = trans.TransId,
                        TpUserName = trans.TpUserName,
                        drawType = trans.drawType,
                        drawDate = ConvertDateTime(trans.drawDate),
                        SettleFlag = trans.SettleFlag
                    });

                foreach (var days in uniqueDrawDates)
                {
                    var getDrawResult = await _apiHLBLL.GetDrawResult(days);
                    if (getDrawResult.hl_Response.ErrorCode == 3)
                    {
                        msg = $"HL Settled Prize : {days:yyyy-MM-dd} +8, The lottery results have not yet been released.";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(getDrawResult)}");
                        break;
                    }
                    
                    var uniqueDraw = getDrawResult.hl_Response.DrawResult.Distinct().ToList();
                    
                    var matchedTransWager = uniqueDraw
                        .Join(updatedDrawDateList,
                            extracted => new { extracted.drawType, extracted.drawDate },
                            hlBet => new { hlBet.drawType, hlBet.drawDate },
                            (extracted, hlBet) => hlBet)
                        .ToList();

                    if (!matchedTransWager.Any()) continue;
                    {
                        try
                        {
                            var result = await _apiHLBLL.GetWinningNumberByDay(days);
                            if (result.hl_Response.ErrorCode == 3 || result.hl_Response.Desc.ToLower().Contains("data not found"))
                            {
                                var updateList = new List<HLSettleInsertData>();  
                                updateList.AddRange(matchedTransWager.Select(loseWager =>
                                    CreateHLWagerData(loseWager.TransId, "NA", "0")));
                                ProcessUpdateWager(updateList);
                                continue;
                            }
                            
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

                            _ = ProcessUpdateWager(insertList);
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {matchedTransWager.Count()} )";
                            _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(matchedTransWager)}\r\n{ex.Message}");
                            return false;
                        }
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
            var msg = "THIRD_PARTY_ERROR_BUSY_TIME_BY_GET_WAGER";
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

        private bool ProcessUpdateWager(IEnumerable<HLSettleInsertData> wagerList)
        {
            var msg = "FATAL_ERROR";
            foreach (var item in wagerList)
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
            return false;
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

        private void InsertNewWager(HLWagerData wagerData)
        {
            _ = _wagerHLService.InsertHLWager(wagerData);
            _ = _tpUnsettleService.InsertTpUnsettle(Tp.HaoLong, GameType.HaoLong_4D, wagerData.BetID);
        }

        private void UpdateWager(HLSettleInsertData wager)
        {
            _ = _wagerHLService.UpdateHLWager(wager);
            _ = _tpUnsettleService.InsertTpUnsettle(Tp.HaoLong, GameType.HaoLong_4D, wager.TransId);
        }

        private DateTime ConvertTimeToPlusEight(DateTime dateTime)
        {

            var result = new DateTimeOffset(dateTime, TimeSpan.Zero).ToOffset(new TimeSpan(8, 0, 0)).DateTime;
            return result;
        }

        private DateTime ConvertDateTime(DateTime dateTime)
        {
            if (dateTime.Hour == 16)
            {
                dateTime = new DateTimeOffset(dateTime, TimeSpan.Zero).ToOffset(new TimeSpan(8, 0, 0)).DateTime;
            }
            else if (dateTime.Hour == 0)
            {
                return dateTime;
            }
            return dateTime;

        }
    

        private DateTime ConvertTimeToUTCTime(DateTime dateTime)
        {
            var result = new DateTimeOffset(dateTime, new TimeSpan(8, 0, 0)).UtcDateTime;
            return result;
        }
        
        #endregion
    }
}