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
        private readonly CommonBLL _commonBLL;
        private readonly INextspinApi _apiNextspinBll;
        private readonly IWagerNextspinService _wagerService;
        private readonly ILogger<NextspinBLL> _logger;
        private readonly List<int> _siteList;
        private readonly Dictionary<int, NextspinConfig> _siteConfigDic;

        public NextspinBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<NextspinBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _wagerService = provider.GetRequiredService<IWagerNextspinService>();
            _apiNextspinBll = provider.GetRequiredService<INextspinApi>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _siteList = _commonBLL.GetSiteByTpGameType((int)GameType.Nextspin_RNG).ToList();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<NextspinConfig>(Tp.Nextspin.ToString());
        }

        public async Task<bool> GetWagersAsync(string startTime, string endTime)
        {
            try
            {
                foreach (var siteId in _siteList)
                {
                    var config = _siteConfigDic[siteId];

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
