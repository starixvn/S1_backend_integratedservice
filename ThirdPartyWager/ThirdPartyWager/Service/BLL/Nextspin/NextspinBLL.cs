using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

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

        public NextspinBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<NextspinBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _wagerService = provider.GetRequiredService<IWagerNextspinService>();
            _apiNextspinBll = provider.GetRequiredService<INextspinApi>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _siteList = _commonBLL.GetSiteByTpGameType((int)GameType.Nextspin_RNG).ToList();
        }

        public async Task<bool> GetWagersAsync(string startTime, string endTime)
        {
            try
            {
                foreach (var siteId in _siteList)
                {
                    var setting = _thirdPartyBLL.GetThirdPartyBySite<NextspinSetting>((int)Tp.Nextspin, siteId);

                    var page = 1;
                    int count;
                    do
                    {
                        var betHishory = await _apiNextspinBll.GetBetHistory(startTime, endTime, page, setting);

                        if (betHishory == null) return false;

                        count = betHishory.PageCount - page;
                        if (betHishory.List != null && betHishory.List.Count > 0)
                        {
                            var result = await _wagerService.InsertWager(betHishory.List);
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
