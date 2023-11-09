using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL.HL
{ 
    public class WagerHLBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiAllBetBLL _apiAllBetBLL;
        private readonly WagerHLBLL _wagerHLService;
        private readonly ILogger<WagerAllBetBLL> _logger;
        private IEnumerable<TpCurrency> _currencyList;
        private readonly int _tpId = (int)Tp.HL;
        private readonly IWebHostEnvironment _env;
        private readonly HLConfig _siteHLConfig;

        public WagerHLBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiAllBetBLL = provider.GetRequiredService<ApiAllBetBLL>();
            _wagerHLService = provider.GetRequiredService<WagerHLBLL>();
            _logger = provider.GetRequiredService<ILogger<WagerAllBetBLL>>();
            _currencyList = provider.GetRequiredService<CommonBLL>().GetTpCurrencyList(_tpId);
            _env = provider.GetRequiredService<IWebHostEnvironment>();
            _siteHLConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<HLConfig>(Tp.Allbet.ToString());
        }

        public async Task<bool> GetWagersAsync(string startTime, string endTime)
        {
            return true;
        }

    }
}