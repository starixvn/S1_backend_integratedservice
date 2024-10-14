using Newtonsoft.Json;

using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerAllBetBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiAllBetBLL _apiAllBetBLL;
        private readonly WagerAllBetService _wagerAllBetService;
        private readonly ILogger<WagerAllBetBLL> _logger;
        private IEnumerable<TpCurrency> _currencyList;
        private readonly int _tpId = (int)Tp.Allbet;
        private readonly IWebHostEnvironment _env;
        private readonly Dictionary<int, AllBetConfig> _siteConfigDic;

        public WagerAllBetBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiAllBetBLL = provider.GetRequiredService<ApiAllBetBLL>();
            _wagerAllBetService = provider.GetRequiredService<WagerAllBetService>();
            _logger = provider.GetRequiredService<ILogger<WagerAllBetBLL>>();
            _currencyList = provider.GetRequiredService<CommonBLL>().GetTpCurrencyList(_tpId);
            _env = provider.GetRequiredService<IWebHostEnvironment>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<AllBetConfig>(Tp.Allbet.ToString());
        }

        public async Task<bool> GetWagersAsync(string startTime, string endTime)
        {
            var msg = "FATAL_ERROR";
            try
            {

                if (_env.IsDevelopment())
                {
                    _currencyList = _currencyList.Take(1);
                }
                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
                foreach (var site in siteList)
                {
                    if (site.TicketKey == 0)
                    {
                        continue;
                    }

                    var _siteAllBetConfig = _siteConfigDic[site.SiteId];

                    foreach (var currency in _currencyList)
                    {
                        try
                        {
                            Thread.Sleep(15000);
                            // pagination
                            var pageIndex = 1;
                            var pageRowCnt = 1;

                            var agent = _siteAllBetConfig.CurrencyAgents.FirstOrDefault(x => x.CurrencyId == currency.TpCurrencyId)?.Agent;
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
                                if (dataWager.resultCode != "OK")
                                {
                                    msg = "THIRD_PARTY_ERROR";
                                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                                    return false;
                                }

                                pageRowCnt = dataWager.data.list.Count();
                                if (pageRowCnt == 0)
                                {
                                    msg = "NO_DATA";
                                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                                    break;
                                }

                                // insert wager to db
                                foreach (var bet in dataWager.data.list.OrderBy(item => item.betTime))
                                {
                                    try
                                    {
                                        _wagerAllBetService.InsertWager(bet);
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


                            // modified bet
                            var modifyWager = _apiAllBetBLL.GetModifiedBetListByTime(agent, startTime, endTime, site.SiteId);
                            if (modifyWager.resultCode != "OK")
                            {
                                msg = "THIRD_PARTY_ERROR";
                                _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(modifyWager)}");
                                return false;
                            }

                        var pageRowCntOfModify = modifyWager.data.total;
                        if (pageRowCntOfModify == 0)
                        {
                            msg = "NO_DATA";
                            _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(modifyWager)}");
                            continue;
                        }

                            // insert wager to db
                            foreach (var bet in modifyWager.data.list)
                            {
                                try
                                {
                                    _wagerAllBetService.InsertWager(bet);
                                }
                                catch (Exception ex)
                                {
                                    msg = $"INSERT_WAGER(WAGERID: {bet.betNum} )";
                                    _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                                    return false;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"{currency.TpCurrencyId}Agent GetWagersAsync error. \r\n{ex.Message}");
                        }
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

        public async Task<(bool Success, bool DoQuick)> UnsettleCheck()
        {
            try
            {
                var checkList = await _wagerAllBetService.GetUnsettleCheckList();
                var doQuick = checkList.Count() == 200;
                foreach (var chcek in checkList)
                {
                    try
                    {
                        var result = _apiAllBetBLL.GetBetByBetNum(chcek.betNum, chcek.client);
                        if (result.resultCode == "OK")
                        {
                            _wagerAllBetService.InsertWager(result.data);
                        }

                    }
                    catch (Exception ex)
                    {
                        var msg = $"INSERT WAGER Error betNum:{chcek.betNum}.";
                        _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(chcek)}\r\n{ex.Message}");
                    }
                }

                _logger.LogInformation($"Allbet Settle Check Success.");
                return (true, doQuick);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Allbet Settle Check FATAL_ERROR.\r\n{ex.Message}");
                return (false, true);
            }
        }
    }
}
