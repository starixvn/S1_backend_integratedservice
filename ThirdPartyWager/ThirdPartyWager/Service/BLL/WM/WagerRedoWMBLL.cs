using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerRedoWMBLL
    {
        private readonly ApiWMBLL _apiWMBLL;
        private readonly WagerWMService _wagerWMService;
        private readonly ILogger<WagerRedoWMBLL> _logger;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly IHostEnvironment _environment;
        private readonly TpUnsettleService _tpUnsettleService;
        private readonly WMConfig _siteConfig;

        public WagerRedoWMBLL(IServiceProvider provider)
        {
            _apiWMBLL = provider.GetRequiredService<ApiWMBLL>();
            _wagerWMService = provider.GetRequiredService<WagerWMService>();
            _logger = provider.GetRequiredService<ILogger<WagerRedoWMBLL>>();
            _tpCurrency = provider.GetRequiredService<CommonBLL>().GetTpCurrencyList((int)Tp.WM);
            _environment = provider.GetRequiredService<IHostEnvironment>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<WMConfig>(Tp.WM.ToString());
        }

        public async Task<bool> RedoWagersAsync(string startTime, string endTime, long startUnix, long endUnix)
        {
            string msg;
            try
            {
                // get api info
                var setting = _siteConfig;

                var reCheck = false;
                foreach (var currency in _tpCurrency)
                {
                    if (!_environment.IsProduction() && currency.TpCurrencyId != "RMB")
                    {
                        continue;
                    }
                    // count interval wagers
                    var getWagerCount = _wagerWMService.GetWagerTotal(startUnix, endUnix, currency.TpCurrencyId);
                    var wagerCount = getWagerCount.wagerCount;

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

                    // db wager count equals api bet count do nothing
                    var totalNum = dataWager.result.Count();
                    if (totalNum == wagerCount)
                    {
                        msg = "MATCH";
                        _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        continue;
                    }

                    // insert wager to db
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

                    reCheck = true;
                }

                msg = "FINISH";
                _logger.LogInformation($"RedoWagersAsync {msg}\r\n{startTime} ~ {endTime}");
                return !(reCheck);
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"RedoWagersAsync {msg}\r\n{ex.Message}");
                return false;
            }
        }
    }
}
