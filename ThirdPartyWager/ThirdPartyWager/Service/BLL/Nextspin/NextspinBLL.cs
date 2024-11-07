using Newtonsoft.Json;
using System.Security.Policy;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public interface INextspinBLL
    {
        Task<bool> GetWagersAsync(string startTime, string endTime);
    }

    public class NextspinBLL : INextspinBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly INextspinApi _apiNextspinBll;
        private readonly IWagerNextspinService _wagerService;
        private readonly ILogger<NextspinBLL> _logger;
        private readonly Dictionary<int, NextspinConfig> _siteConfigDic;
        private readonly int _tpId = (int)Tp.Nextspin;

        public NextspinBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<NextspinBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _wagerService = provider.GetRequiredService<IWagerNextspinService>();
            _apiNextspinBll = provider.GetRequiredService<INextspinApi>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<NextspinConfig>(Tp.Nextspin.ToString());
        }

        public async Task<bool> GetWagersAsync(string startTime, string endTime)
        {
            try
            {
                var _siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
                foreach (var site in _siteList)
                {
                    if (site.TicketKey == 0)
                    {
                        continue;
                    }

                    if (_siteConfigDic.TryGetValue(site.SiteId, out var config))
                    {
                        if(config == null)
                        {
                            _logger.LogError($"siteId={site.SiteId}, siteConfigDic config={JsonConvert.SerializeObject(config)}");
                            continue;
                        }

                        var page = 1;
                        int count;
                        do
                        {
                            var betHistory = await _apiNextspinBll.GetBetHistory(startTime, endTime, page, config);

                            if (betHistory == null) return false;

                            count = betHistory.PageCount - page;
                            if (betHistory.List != null && betHistory.List.Count > 0)
                            {
                                var result = await _wagerService.InsertWager(betHistory.List);
                                page++;
                            }
                        }
                        while (count > 0);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"insert TpWagerNextspin failed: {ex.Message}");
                return false;
            }
        }
    }
}
