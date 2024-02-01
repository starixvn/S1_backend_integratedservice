using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerWMBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiWMBLL _apiWMBLL;
        private readonly WagerWMService _wagerWMService;
        private readonly ILogger<WagerWMBLL> _logger;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly IHostEnvironment _environment;
        private readonly TpUnsettleService _tpUnsettleService;
        private readonly WMConfig _siteConfig;

        public WagerWMBLL(IServiceProvider provider, IHostEnvironment environment)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiWMBLL = provider.GetRequiredService<ApiWMBLL>();
            _wagerWMService = provider.GetRequiredService<WagerWMService>();
            _logger = provider.GetRequiredService<ILogger<WagerWMBLL>>();
            _tpCurrency = provider.GetRequiredService<CommonBLL>().GetTpCurrencyList((int)Tp.WM);
            _environment = environment;
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<WMConfig>(Tp.WM.ToString());
        }

        public async Task<bool> GetWagersAsync(string startTime, string endTime)
        {
            string msg;
            try
            {
                // get api info
                var setting = _siteConfig;

                foreach (var currency in _tpCurrency)
                {
                    if (!_environment.IsProduction() && currency.TpCurrencyId != "RMB")
                    {
                        continue;
                    }
                    // get wager
                    var dataWager = await _apiWMBLL.GetBetListByTime(setting, currency.TpCurrencyId, startTime, endTime);

                    // error condition
                    var errorCode = dataWager.errorCode;

                    if (errorCode == 107)
                    {
                        msg = "NO_DATA";
                        _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        continue;
                    }

                    if (errorCode != 0)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return false;
                    }


                    foreach (var bet in dataWager.result)
                    {
                        try
                        {
                            bet.Currency = currency.TpCurrencyId;
                            var result = _wagerWMService.InsertWager(bet);
                            if (result > 0)
                            {
                                _tpUnsettleService.InsertTpUnsettle(Tp.WM, GameType.WM_LiveCasino, bet.TransId);
                            }
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {bet.betId} )";
                            _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                            return false;
                        }
                    }
                }

                msg = "FINISH";
                _logger.LogInformation($"GetWagersAsync {msg}\r\n{startTime} ~ {endTime}");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"GetWagersAsync {msg}\r\n{ex.Message}");
                return false;
            }
        }
    }
}
