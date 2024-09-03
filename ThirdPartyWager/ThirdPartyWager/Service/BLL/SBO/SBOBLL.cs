using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public interface ISBOBLL
    {
        Task<bool> GetWagersAsync(string startTime, string endTime, SBOPortfolio portfolio);
    }

    public class SBOBLL : ISBOBLL
    {
        private readonly IWagerSBOService _wagerSboService;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly IApiSBOBLL _apiSboBll;
        private readonly ILogger<SBOBLL> _logger;
        private readonly int _tpId = (int)Tp.SBO;
        private readonly Dictionary<int, SBOConfig> _siteConfigDic;

        public SBOBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<SBOBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _wagerSboService = provider.GetRequiredService<IWagerSBOService>();
            _apiSboBll = provider.GetRequiredService<IApiSBOBLL>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<SBOConfig>(Tp.SBO.ToString());
        }

        public async Task<bool> GetWagersAsync(string startTime, string endTime, SBOPortfolio portfolio)
        {
            var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
            foreach (var siteData in siteList)
            {
                if (siteData.TicketKey == 0)
                {
                    _logger.LogError($"SiteId={siteData.SiteId}, no execute");
                    continue;
                }

                // 分Site取得TpList
                var sboSetting = _siteConfigDic[siteData.SiteId];

                var sboWagers = await _apiSboBll.GetBetListByModifyDateAsync(startTime, endTime, portfolio, sboSetting);

                if (sboWagers == null) return false;

                if (sboWagers.error.id != 0)
                {
                    _logger.LogError($"get betList by modify date failed: {sboWagers.error.msg}");
                    return false;
                }

                try
                {
                    if (sboWagers.result.Count > 0)
                    {
                        foreach (var wager in sboWagers.result)
                        {
                            var result = await _wagerSboService.InsertWager(wager);
                            if (result < 1)
                            {
                                _logger.LogError($"insert TpWagerSBO failed:{JsonConvert.SerializeObject(wager)}");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"insert TpWagerSBO failed: {e.Message}");
                    return false;
                }
            }

            return true;
        }
    }
}