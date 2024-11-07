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
        private readonly ThirdPartyBLL _thirdPartyBLL;

        public WagerHLBLL(IServiceProvider provider)
        {
            _apiHLBLL = provider.GetRequiredService<ApiHLBLL>();
            _wagerHLService = provider.GetRequiredService<WagerHLService>();
            _logger = provider.GetRequiredService<ILogger<WagerHLBLL>>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        public async Task<bool> GetWagersAsync(IEnumerable<HLWagerTrans> unSettleData, List<DateTime> dayList)
        {
            var msg = "FATAL_ERROR";
            var guid = Guid.NewGuid().ToString();

            try
            {
                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
                foreach (var siteInfo in siteList)
                {
                    if (siteInfo.TicketKey == 0)
                    {
                        continue;
                    }
                    var siteId = siteInfo.SiteId;

                    //抓單前先試打，若是卡維護就直接false，免得進exception卡時間。
                    if (!await TryGetWagersForFirstDay(siteId, dayList, guid)) return false;

                    var allWagerList = await FetchWagersForDays(siteId, dayList, guid);

                    //沒單就不跑下面的了
                    if (!allWagerList.Any())
                        return true;

                    if (unSettleData.Any())
                    {
                        allWagerList = HLCheckWager(unSettleData, allWagerList);
                    }

                    var lastWagerDate = !unSettleData.Any() ? DateTime.Now.AddMonths(-1) : unSettleData.MinBy(x => x.dateTime).dateTime;

                    var allWagerValid = allWagerList
                        .Where(wager => ConvertTimeToUTCTime(wager.DateTime) > lastWagerDate)
                        .OrderBy(wager => wager.DrawDate)
                        .ToList();

                    foreach (var wagerData in allWagerValid)
                    {
                        InsertNewWager(wagerData);
                    }
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
                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
                foreach (var siteInfo in siteList)
                {
                    if (siteInfo.TicketKey == 0)
                    {
                        continue;
                    }
                    var siteId = siteInfo.SiteId;

                    var nowDate = DateTime.Now;
                    //結算前一樣試打 要不然一定出事
                    var tryGetWinNumber = await _apiHLBLL.GetWinningNumberByDay(siteId, nowDate);
                    if (tryGetWinNumber.hl_Response.ErrorCode == 998)
                    {
                        msg = "THIRD_PARTY_ERROR_BUSY_TIME_BY_SETTLED";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(tryGetWinNumber)}");
                        return false;
                    }
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

        public async Task<bool> SetSettledWager(IEnumerable<HLWagerTrans> unSettleData)
        {
            var msg = "FATAL_ERROR";
            var guid = Guid.NewGuid().ToString();
            try
            {
                var nowDate = DateTime.Now;
                _logger.LogInformation($"HaoLong SetSettledWager Job Time : +0 = {nowDate:yyyy-MM-ddTHH:mm:ss}");

                //沒結算單就不跑
                if (!unSettleData.Any())
                {
                    return true;
                }

                var uniqueDrawDates = unSettleData
                    .Where(wager => wager.SettleFlag == 0)
                    .Select(wager => ConvertDateTime(wager.drawDate).Date)
                    .Distinct()
                    .ToList();

                var updatedDrawDateList = unSettleData
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

                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
                foreach (var siteInfo in siteList)
                {
                    if (siteInfo.TicketKey == 0)
                    {
                        continue;
                    }
                    var siteId = siteInfo.SiteId;

                    foreach (var days in uniqueDrawDates)
                    {
                        var getDrawResult = await _apiHLBLL.GetDrawResult(siteId, days);
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
                                var result = await _apiHLBLL.GetWinningNumberByDay(siteId, days);
                                if (result.hl_Response.ErrorCode == 3)
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

        private async Task<bool> TryGetWagersForFirstDay(int siteId, IEnumerable<DateTime> dayList, string guid)
        {
            var tryNowDay = dayList.FirstOrDefault().ToString("yyyy-MM-dd");
            var tryGetWager = await _apiHLBLL.GetBetListByTime(siteId, tryNowDay, guid, 10);
            if (tryGetWager.hl_Response.ErrorCode != 998) return true;
            var msg = "THIRD_PARTY_ERROR_BUSY_TIME_BY_GET_WAGER";
            _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(tryGetWager)}");
            return false;
        }

        private async Task<IEnumerable<HLWagerData>> FetchWagersForDays(int siteId, IEnumerable<DateTime> dayList, string guid)
        {
            var msg = "FATAL_ERROR";
            var allWagerList = new List<HLWagerData>();

            foreach (var day in dayList)
            {
                var getDay = day.ToString("yyyy-MM-dd");

                var page = 1;
                var isDone = false;

                while (!isDone)
                {
                    var wagerData = await _apiHLBLL.GetBetListByTime(siteId, getDay, guid, page);

                    var dataWager = wagerData.hl_Response;

                    if (dataWager.ErrorCode == 3 && dataWager.Desc == "Data NOT Found")
                    {
                        msg = "NO_DATA";
                        _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)} ,Page {page} ,Date:{getDay}");
                        isDone = true;
                        continue;
                    }

                    if (dataWager.ErrorCode != 0)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)} , Date:{getDay}");
                        isDone = true;
                        continue;
                    }

                    allWagerList.AddRange(dataWager.WagerData);

                    page++;
                }
            }

            var distinctWagerData = allWagerList
                .GroupBy(wager => wager.BetID)
                .Select(group => group.First())
                .OrderBy(wager => wager.DrawDate)
                .ToList();

            return distinctWagerData;
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

        private static HLSettleInsertData CreateHLWagerData(string betId, string strikePosition, string settleAmount)
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
            var result = _wagerHLService.InsertHLWager(wagerData);
            if (result)
            {
                _ = _tpUnsettleService.InsertTpUnsettle(Tp.HaoLong, GameType.HaoLong_4D, wagerData.BetID);
            }
        }

        private static DateTime ConvertDateTime(DateTime dateTime)
        {
            switch (dateTime.Hour)
            {
                case 16:
                    dateTime = new DateTimeOffset(dateTime, TimeSpan.Zero).ToOffset(new TimeSpan(8, 0, 0)).DateTime;
                    break;
                case 0:
                    return dateTime;
            }

            return dateTime;
        }

        private static DateTime ConvertTimeToUTCTime(DateTime dateTime)
        {
            var result = new DateTimeOffset(dateTime, new TimeSpan(8, 0, 0)).UtcDateTime;
            return result;
        }

        private static IEnumerable<HLWagerData> HLCheckWager(IEnumerable<HLWagerTrans> unSettleData, IEnumerable<HLWagerData> wagerData)
        {
            var unSettleDataIds = new HashSet<string>(unSettleData.Select(uns => uns.TransId));
            var newWagerData = wagerData.Where(wd => !unSettleDataIds.Contains(wd.BetID)).ToList();

            return newWagerData;
        }

        #endregion
    }
}