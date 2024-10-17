using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerRedoAllBetBLL
    {
        private readonly ApiAllBetBLL _apiAllBetBLL;
        private readonly WagerAllBetService _wagerAllBetService;
        private readonly ILogger<WagerRedoAllBetBLL> _logger;
        private IEnumerable<TpCurrency> _currencyList;
        private readonly int _tpId = (int)Tp.Allbet;
        private readonly IWebHostEnvironment _env;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;
        private readonly Dictionary<int, AllBetConfig> _siteConfigDic;
        public WagerRedoAllBetBLL(IServiceProvider provider)
        {
            _apiAllBetBLL = provider.GetRequiredService<ApiAllBetBLL>();
            _wagerAllBetService = provider.GetRequiredService<WagerAllBetService>();
            _logger = provider.GetRequiredService<ILogger<WagerRedoAllBetBLL>>();
            _currencyList = provider.GetRequiredService<CommonBLL>().GetTpCurrencyList(_tpId);
            _env = provider.GetRequiredService<IWebHostEnvironment>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<AllBetConfig>(Tp.Allbet.ToString());
        }

        public async Task<bool> RedoWagersAsync(int tpId, string startTime, string endTime, long startUnix, long endUnix)
        {
            var msg = "FATAL_ERROR";
            try
            {
                // count interval wagers
                var getWagerCount = _wagerAllBetService.GetWagerTotal(startUnix, endUnix).ToList();

                if (_env.IsDevelopment())
                {
                    _currencyList = _currencyList.Take(1);
                }
                var siteList = _thirdPartyBLL.GetTpListByTpId(tpId);
                var serverList = _commonBLL.GetSiteList();
                foreach (var site in siteList)
                {
                    if (site.TicketKey == 0)
                    {
                        continue;
                    }

                    var _siteAllBetConfig = _siteConfigDic[site.SiteId];
                    var currencyList = JsonConvert.DeserializeObject<IEnumerable<int>>(serverList.First(x => x.SiteId == site.SiteId).CurrencyList);

                    foreach (var currency in _currencyList.Where(x => currencyList.Contains(x.CurrencyId)))
                    {
                        try
                        {
                            // pagination
                            var pageIndex = 1;
                            var pageRowCnt = 1;

                            var agent = _siteAllBetConfig.CurrencyAgents.FirstOrDefault(x => x.CurrencyId == currency.TpCurrencyId)?.Agent;
                            var wagerCount = getWagerCount.Where(x => x.MainSiteId == site.SiteId && x.TpCurrencyId == currency.TpCurrencyId).Sum(x => x.wagerCount);
                            while (pageRowCnt >= 1)
                            {
                                // bet 
                                var dataWager = _apiAllBetBLL.GetBetListByTime(agent, startTime, endTime, pageIndex, site.SiteId);
                                if (dataWager == null)
                                {
                                    msg = "THIRD_PARTY_ERROR";
                                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                                    return false;
                                }

                                // error condition
                                var errorCode = dataWager.resultCode;
                                if (errorCode != "OK")
                                {
                                    msg = "THIRD_PARTY_ERROR";
                                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                                    return false;
                                }

                                pageRowCnt = dataWager.data.list.Count();
                                var totalNum = dataWager.data.total;
                                if (pageRowCnt == 0)
                                {
                                    msg = "NO_DATA";
                                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                                    return true;
                                }

                                // db wager count equals api bet count do nothing
                                if (totalNum == wagerCount)
                                {
                                    msg = "MATCH";
                                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                                    return true;
                                }

                                // insert wager to db
                                foreach (var bet in dataWager.data.list)
                                {
                                    try
                                    {
                                        var r = _wagerAllBetService.InsertWager(bet);
                                    }
                                    catch (Exception ex)
                                    {
                                        msg = $"INSERT_WAGER(WAGERID: {bet.betNum} )";
                                        _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                                        return false;
                                    }
                                }
                                pageIndex++;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"{currency.TpCurrencyId}Agent Get RedoWagersAsync error. \r\n{ex.Message}");
                        }
                    }
                }
                
                msg = "FINISH";
                _logger.LogInformation($"RedoWagersAsync {msg}\r\n{startTime} ~ {endTime}");
                return false;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"RedoWagersAsync {msg}\r\n{ex.Message}");
                return false;
            }
        }
    }
}
