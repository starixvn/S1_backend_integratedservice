using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerEdayBLL
    {
        private readonly ApiEdayBLL _apiEdayBLL;
        private readonly WagerEdayService _wagerEdayService;
        private readonly ILogger<WagerEdayBLL> _logger;
        private readonly int _tpId = (int)Tp.Eday;
        private readonly TpUnsettleService _tpUnsettleService;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly Dictionary<int, EdayConfig> _siteConfigDic;
        
        public WagerEdayBLL(IServiceProvider provider)
        {
            _apiEdayBLL = provider.GetRequiredService<ApiEdayBLL>();
            _wagerEdayService = provider.GetRequiredService<WagerEdayService>();
            _logger = provider.GetRequiredService<ILogger<WagerEdayBLL>>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<EdayConfig>(Tp.Eday.ToString());
        }

        public async Task<bool> GetWagersAsync(string startTime, string endTime)
        {
            var msg = "FATAL_ERROR";
            startTime += ".000+00:00";
            endTime += ".000+00:00";
            
            try
            {
                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
                foreach (var site in siteList)
                {
                    if (site.TicketKey == 0)
                    {
                        continue;
                    }
                    
                    var siteConfig = _siteConfigDic[site.SiteId];
                    int? page = 0;
                    int? totalPage = 0;
                    while (page <= totalPage)
                    {
                        var wagerEdayData = await _apiEdayBLL.GetBetListByTime(startTime, endTime, siteConfig, page);
    
                        if (wagerEdayData.ResultCode != 0)
                        {
                            msg = "THIRD_PARTY_ERROR";
                            _logger.LogError($"Eday {msg}\r\n{JsonConvert.SerializeObject(wagerEdayData)}");
                            return false;
                        }
    
                        if (wagerEdayData.Data.Count == 0 || wagerEdayData.Data.Items.Count == 0)
                        {
                            msg = "NO_DATA";
                            _logger.LogInformation($"Eday:{msg}\r\nSite:{site.SiteId}\r\n{JsonConvert.SerializeObject(wagerEdayData)}");
                            break;
                        }
                        totalPage = wagerEdayData.Data.TotalPages;
    
                        foreach (var wager in wagerEdayData.Data.Items)
                        {
                            try
                            {
                                wager.BillId = wager.BillId.TrimStart('0');
                                //後續Finance計算用
                                wager.WagerType = 1;
                                InsertNewEdayWager(wager);
                            }
                            catch (Exception ex)
                            {
                                msg = $"INSERT_WAGER(WAGERID: {wager.BillId} )";
                                _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(wager)}\r\n{ex.Message}");
                                return false;
                            }
                        }
                        
                        //注單全部塞完之後 在檢查(RoundCode + BillId + Player)，
                        //若是確認全結算再多塞一筆(RoundCode + Player)的BillId跑結算
                        var edaySettleCheck = GetEdayFullySettledRounds(wagerEdayData.Data.Items);
                        foreach (var item in edaySettleCheck)
                        {
                            try
                            {
                                InsertNewEdayWager(item);
                            }
                            catch (Exception ex)
                            {
                                msg = $"INSERT_WAGER(WAGERID: {item.BillId} )";
                                _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(item)}\r\n{ex.Message}");
                                return false;
                            }
                        }
                        
                        Thread.Sleep(2000);
                        page++; 
                    }
                }
                
                msg = "FINISH";
                _logger.LogInformation($"GetWagersAsync {msg}\r\n{DateTime.Now:yyyy-MM-dd} ~ {DateTime.Now:yyyy-MM-dd}");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"GetWagersAsync {msg}\r\n{ex.Message}");
                return false;
            }
        }
        
        public async Task<bool> GetEdayBillDetailAsync()
        {
            var msg = "FATAL_ERROR";
            
            try
            {
                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
                foreach (var site in siteList)
                {
                    if (site.TicketKey == 0)
                    {
                        continue;
                    }
                    
                    var siteConfig = _siteConfigDic[site.SiteId];
                    
                    //因為目前最大結算日為6天，固定拿7天內未結算的單去更新
                    //拿billId取注單細單並更新回Bettrans，到時候TpWager拉的時候就直接拿billId結算
                    //Deal單號拿來純紀錄 以免造成後續問題發生跟報表會有異常
                    var needUpdateEdayBillId = _wagerEdayService.GetEdayBillIdByTime();
                    
                    foreach (var item in needUpdateEdayBillId)
                    {
                        var wagerEdayData = await _apiEdayBLL.GetBillDetail(item.billId,siteConfig);
                        if (wagerEdayData.ResultCode != 0)
                        {
                            msg = "THIRD_PARTY_ERROR";
                            _logger.LogError($"Eday {msg}\r\n{JsonConvert.SerializeObject(wagerEdayData)}");
                            return false;
                        }
    
                        //結算才做下一筆 其他的一律略過
                        if (wagerEdayData.Data.BillStatus != 200)
                        {
                            continue;
                        }
                        
                        var ticketStatus = 0;
                        ticketStatus = wagerEdayData.Data.TotalWin > 0.00m ? 1 : 2;
                        _wagerEdayService.UpdateEdayBillId(wagerEdayData.Data,ticketStatus);
                    }
                }
                
                msg = "FINISH";
                _logger.LogInformation($"GetEdayBillDetailAsync {msg}\r\n{DateTime.Now:yyyy-MM-dd} ~ {DateTime.Now:yyyy-MM-dd}");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"GetEdayBillDetailAsync {msg}\r\n{ex.Message}");
                return false;
            }
        }
        
        private void InsertNewEdayWager(WagerEdayBillData wagerData)
        {
            var result = _wagerEdayService.InsertEdayWager(wagerData);
            //Deal單不用丟結算 純紀錄
            if (result == 1 && wagerData.WagerType == 1)
            {
                _ = _tpUnsettleService.InsertTpUnsettle(Tp.Eday, GameType.Eday_Lottery, wagerData.BillId);
            }
        }

        private IEnumerable<WagerEdayBillData> GetEdayFullySettledRounds(IEnumerable<WagerEdayBillData> wagers)
        {
            var result = new List<WagerEdayBillData>();

            var groupedByRound = wagers.GroupBy(b => new { b.RoundCode, b.Player });

            foreach (var group in groupedByRound)
            {
                var billStatuses = group.Select(b => b.BillStatus).ToList();
            
                var allSettled = billStatuses.All(status => status == 200 || status == 100);
                var allHundred = billStatuses.All(status => status == 100);

                if (group.Any(b => b.ItemCount == 10) && allHundred)
                {
                    continue;
                }

                if (allSettled)
                {
                    var settledWager = CreateEdaySettleData(group);
                    if (settledWager != null)
                    {
                        result.Add(settledWager);
                    }
                }
            }

            return result;
        }

        private static WagerEdayBillData CreateEdaySettleData(IEnumerable<WagerEdayBillData> wager)
        {
            //共通資料拿第一筆的資料就好
            var wagerFirst = wager.FirstOrDefault();
            
            return new WagerEdayBillData
            {
                BillId = $"{wagerFirst.RoundCode}_{wagerFirst.Player}",
                Player = wagerFirst.Player,
                GameCode = wagerFirst.GameCode,
                RoundCode = wagerFirst.RoundCode,
                BillStatus = 200,
                CreatedTime = wagerFirst.CreatedTime,
                SettlementTime = wagerFirst.SettlementTime,
                EndTime = wagerFirst.EndTime,
                ItemCount = wager.Sum(x => x.ItemCount),
                TotalBet = wager.Sum(x => x.TotalBet),
                TotalWin = wager.Sum(x => x.TotalWin),
                TotalNet = wager.Sum(x => x.TotalNet),
                Items = null,
                //WagerType = 2，算帳時不會被記錄
                WagerType = 2,
            };
        }
    }
}