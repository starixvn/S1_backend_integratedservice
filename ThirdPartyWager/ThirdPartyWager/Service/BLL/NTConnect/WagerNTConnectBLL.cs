using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerNTConnectBLL
    {
        private readonly ApiNTConnectBLL _apiNTConnectBLL;
        private readonly WagerNTConnectService _wagerNTConnectService;
        private readonly ILogger<WagerNTConnectBLL> _logger;
        private readonly int _tpId = (int)Tp.NTConnect;
        private readonly TpUnsettleService _tpUnsettleService;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly Dictionary<int, NTConnectConfig> _siteConfigDic;


        public WagerNTConnectBLL(IServiceProvider provider)
        {
            _apiNTConnectBLL = provider.GetRequiredService<ApiNTConnectBLL>();
            _wagerNTConnectService = provider.GetRequiredService<WagerNTConnectService>();
            _logger = provider.GetRequiredService<ILogger<WagerNTConnectBLL>>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<NTConnectConfig>(Tp.NTConnect.ToString());
        }

        public async Task<bool> GetWagersAsync(string startTime, string endTime)
        {
            var msg = "FATAL_ERROR";
            try
            {
                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
                foreach (var site in siteList)
                {
                    if (site.TicketKey == 0)
                    {
                        _logger.LogError($"SiteId={site.SiteId}, no execute");
                        continue;
                    }

                    var siteConfig = _siteConfigDic[site.SiteId];
                    var apiUrl = siteConfig.ApiUrl;
                    foreach (var currencyKey in siteConfig.CurrencyList)
                    {
                        var clientKey = currencyKey.ClientKey;
                        var secretKey = currencyKey.SecretKey;
                        int? page = 1;
                        int? totalPage = 1;
                        while (page <= totalPage)
                        {
                            var apiParams = (apiUrl, clientKey, secretKey);
                            var wager93data = await _apiNTConnectBLL.GetBetListByTime(startTime, endTime,apiParams,page);
                            
                            if (wager93data.status == "error")
                            {
                                msg = "THIRD_PARTY_ERROR";
                                _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(wager93data)}");
                                return false;
                            }
                            
                            if (wager93data.totalpage == null || wager93data.page == null)
                            {
                                msg = "NO_DATA";
                                    _logger.LogInformation($"93Connect:{msg}\r\nSite:{siteConfig.SiteName},Currency:{currencyKey.CurrencyCode}\r\nSiteCurrency:{clientKey}\r\n{JsonConvert.SerializeObject(wager93data)}");
                                    break;
                            }
                            totalPage = wager93data.totalpage;

                            
                            foreach (var wager in wager93data.data)
                            {
                                try
                                {
                                    InsertNew93Wager(wager);
                                }
                                catch (Exception ex)
                                {
                                    msg = $"INSERT_WAGER(WAGERID: {wager.ticket} )";
                                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(wager)}\r\n{ex.Message}");
                                    return false;
                                }
                            }
                            
                            //5秒後在抓下一個currency
                            Thread.Sleep(5000);
                            page++; 
                        }
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
        
        private void InsertNew93Wager(Wager93Data wagerData)
        {
            var result = _wagerNTConnectService.InsertNTConnectWager(wagerData);
            if (result == 1)
            {
                _ = _tpUnsettleService.InsertTpUnsettle(Tp.NTConnect, GameType.NTConnect_Games, wagerData.ticket);
            }
        }
    }
}