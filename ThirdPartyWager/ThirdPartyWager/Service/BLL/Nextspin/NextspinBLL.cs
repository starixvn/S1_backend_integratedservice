using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL.Nextspin
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
        private readonly List<int> _siteList;
        public NextspinBLL(ThirdPartyBLL thirdPartyBLL, INextspinApi apiNextspinBll, IWagerNextspinService wagerService, CommonBLL commonBLL, ILogger<NextspinBLL> logger)
        {
            _thirdPartyBLL = thirdPartyBLL;
            _apiNextspinBll = apiNextspinBll;
            _wagerService = wagerService;
            _logger = logger;
            _siteList = commonBLL.GetSiteByTpGameType((int)GameType.Nextspin_RNG).ToList();
        }
        public async Task<bool> GetWagersAsync(string startTime, string endTime)
        {
            try
            {
                foreach (var siteId in _siteList)
                {
                    var dictSetting = _thirdPartyBLL.GetThirdPartyBySite((int)Tp.Nextspin, siteId);
                    var setting = GetNextspinSetting(dictSetting);

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
        private static NextspinSetting GetNextspinSetting(Dictionary<string, string> setting)
        {
            return new NextspinSetting()
            {
                LaunchUrl = setting["LaunchUrl"],
                ApiUrl = setting["ApiUrl"],
                MerchantCode = setting["MerchantCode"],
                SecretKey = setting["SecretKey"]
            };
        }
    }
}
