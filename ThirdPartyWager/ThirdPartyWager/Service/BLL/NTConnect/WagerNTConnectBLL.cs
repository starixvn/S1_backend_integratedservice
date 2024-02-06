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
        private readonly WorkerType workerType = WorkerType.MainWager;
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
            var guid = Guid.NewGuid().ToString();
            try
            {
                foreach (var siteConfig in _siteConfigDic.Keys.Select(site => _siteConfigDic[site]))
                {
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
                            var wager93data = await _apiNTConnectBLL.GetBetListByTime(startTime, endTime,guid,apiParams,page);
                            if (wager93data.totalpage == null || wager93data.page == null)
                            {
                                msg = "NO_DATA";
                                _logger.LogInformation($"93Connect:{msg}\r\nSiteCurrency:{clientKey}\r\n{JsonConvert.SerializeObject(wager93data)}");
                                break;
                            }
                            totalPage = wager93data.totalpage;

                            foreach (var wager in wager93data.data)
                            {
                                InsertNew93Wager(wager);
                            }
                            page++;
                        }
                    }
                }
                
                //var wagerData = await _apiNTConnectBLL.GetBetListByTime(startTime, endTime,guid);
                
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
                _ = _tpUnsettleService.InsertTpUnsettle(Tp.NTConnect, GameType.NTConnect_Lottery, wagerData.ticket);
            }
        }
    }
}