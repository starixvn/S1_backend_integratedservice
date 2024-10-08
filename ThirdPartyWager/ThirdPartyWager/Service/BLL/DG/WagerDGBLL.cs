using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{

    public class WagerDGBLL
    {
        private readonly ApiDGBLL _apiDGBLL;
        private readonly WagerDGService _wagerDGService;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ILogger<WagerDGBLL> _logger;
        private readonly DGConfig _siteConfig;
        private readonly Tp _thisTp;
        private readonly Dictionary<int, DGConfig> _siteConfigDic;

        public WagerDGBLL(IServiceProvider provider)
        {
            _thisTp = Tp.DG; 
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiDGBLL = provider.GetRequiredService<ApiDGBLL>();
            _wagerDGService = provider.GetRequiredService<WagerDGService>();
            _logger = provider.GetRequiredService<ILogger<WagerDGBLL>>();
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<DGConfig>(Tp.DG.ToString());
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<DGConfig>(_thisTp.ToString());
        }

        public async Task<bool> GetWagersAsync()
        {
            var msg = "FATAL_ERROR";
            var wagerCount = 0;
            try
            {
                var siteList = _thirdPartyBLL.GetTpListByTpId((int)_thisTp);
                foreach (var site in siteList)
                {
                    if (site.TicketKey == 0)
                    {
                        continue;
                    }

                    // 分Site取得TpList
                    // get dg api info
                    var setting = _siteConfigDic[site.SiteId];
                    var wagerIds = new List<long>();
                    // get wagers
                    var dataWager = await _apiDGBLL.GetBetList(setting);
                    var errorCode = dataWager.codeId;
                    if (errorCode != 0)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{site.SiteId} - {msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return false;
                    }

                    if (dataWager.list == null || dataWager.list.Count() == 0)
                    {
                        msg = "NO_DATA";
                        _logger.LogInformation($"{site.SiteId} - {msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        continue;
                    }
                    
                    // insert wager to db
                    foreach (var bet in dataWager.list)
                    {
                        try
                        {
                            await _wagerDGService.InsertWagerDG(bet);
                            wagerIds.Add(bet.id);
                        }
                        catch (Exception ex)
                        {
                            msg = $"{site.SiteId} - INSERT_WAGER(WAGERID: {bet.id} )";
                            _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                            continue;
                        }
                        
                        // mark wager
                        await _apiDGBLL.MarkGotBetList(setting, wagerIds);
                        
                        msg = "FINISH";
                        _logger.LogInformation($"{site.SiteId} - GetWagersAsync {msg}");
                    }
                    wagerCount += dataWager.list.Count();
                }
                //總單量超過1000代表有大量單，所有商號直接繼續拉下一批。
                return wagerCount >= 1000;
                
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"GetWagersAsync {msg}\r\n{ex.Message}");
                return true;
            }
        }
    }
}
