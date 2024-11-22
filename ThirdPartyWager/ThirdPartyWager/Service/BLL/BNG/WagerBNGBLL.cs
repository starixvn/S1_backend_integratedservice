using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerBNGBLL
    {
        private readonly ILogger<WagerBNGBLL> _logger;
        private readonly ApiBNGBLL _apiBNGBLL;
        private readonly WagerBNGService _wagerBNGService;
        private readonly Dictionary<int, BNGConfig> _siteConfigDic;
        private readonly int _tpId = (int)Tp.BNG;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        public WagerBNGBLL(IServiceProvider provider)
        {
            _logger = provider.GetService<ILogger<WagerBNGBLL>>();
            _apiBNGBLL = provider.GetRequiredService<ApiBNGBLL>();
            _wagerBNGService = provider.GetRequiredService<WagerBNGService>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<BNGConfig>(Tp.BNG.ToString());
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        public async Task GetWagerAsync(string startTime, string endTime)
        {
            string msg;
            var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);

            foreach (var site in siteList)
            {

                if (site.TicketKey == 0)
                {
                    continue;
                }

                var _siteBNGConfig = _siteConfigDic[site.SiteId];

                var wagerList = await _apiBNGBLL.GetRecordListAsync(_siteBNGConfig, startTime, endTime);

                if (wagerList.items != null)
                {
                    foreach (var item in wagerList.items)
                    {
                        try
                        {
                            await _wagerBNGService.InsertWager(item);
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(round_id: {item.round_id}, transaction_id: {item.transaction_id} )";
                            _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(item)}\r\n{ex.Message}");
                        }
                    }
                }
            }          
        }
    }
}
