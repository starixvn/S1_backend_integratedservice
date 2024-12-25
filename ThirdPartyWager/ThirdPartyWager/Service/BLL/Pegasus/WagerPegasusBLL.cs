using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerPegasusBLL
    {
        private readonly ApiPegasusBLL _apiPegasusBll;
        private readonly WagerPegasusService _wagerPegasusService;
        private readonly ILogger<WagerPegasusBLL> _logger;
        private readonly CommonBLL _commonBll;

        public WagerPegasusBLL(IServiceProvider provider)
        {
            _apiPegasusBll = provider.GetRequiredService<ApiPegasusBLL>();
            _wagerPegasusService = provider.GetRequiredService<WagerPegasusService>();
            _logger = provider.GetRequiredService<ILogger<WagerPegasusBLL>>();
            _commonBll = provider.GetRequiredService<CommonBLL>();
        }

        public async Task<bool> GetWagersAsync(string startTime, string endTime, int siteId, Dictionary<string, string> setting)
        {
            PegasusConfig(setting, out List<TpCurrencyConfig> currencyConfigList);

            foreach (var currencyConfig in currencyConfigList)
            {
                string msg;
                try
                {
                    // pagination
                    var page = 1;
                    var totalPage = 1;
                    while (page <= totalPage)
                    {
                        // get wager
                        var dataWager = await _apiPegasusBll.GetBetListByTime(setting["ApiUrl"], currencyConfig.SecretKey, currencyConfig.MerchantID, startTime, endTime, page);

                        // error condition
                        var errorCode = dataWager.Code;

                        if (errorCode != 0)
                        {
                            msg = "THIRD_PARTY_ERROR";
                            _logger.LogError($"{msg}\r\nSiteId={siteId},Currncy:{currencyConfig.Currency}\r\n{JsonConvert.SerializeObject(dataWager)}");
                            return false;
                        }

                        var totalNum = dataWager.Data.TotalCount;
                        totalPage = dataWager.Data.TotalPage;

                        if (totalNum == 0)
                        {
                            msg = "NO_DATA";
                            _logger.LogInformation($"{msg}\r\nSiteId={siteId},Currncy:{currencyConfig.Currency}\r\n{JsonConvert.SerializeObject(dataWager)}");
                            continue;
                        }

                        foreach (var bet in dataWager.Data.Items)
                        {
                            try
                            {
                                _wagerPegasusService.InsertWager(bet);
                            }
                            catch (Exception ex)
                            {
                                msg = $"INSERT_WAGER(WAGERID: {bet.TransactionNo} )";
                                _logger.LogError($"{msg}\r\nSiteId={siteId},Currncy:{currencyConfig.Currency}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                                return false;
                            }
                        }
                        page++;
                    }

                    msg = "FINISH";
                    _logger.LogInformation($"GetWagersAsync {msg}\r\nSiteId={siteId},Currncy:{currencyConfig.Currency}\r\n{startTime} ~ {endTime}");
                }
                catch (Exception ex)
                {
                    msg = "FATAL_ERROR";
                    _logger.LogError(ex, $"GetWagersAsync {msg}\r\nSiteId={siteId},Currncy:{currencyConfig.Currency}\r\n{ex.Message}");
                    return false;
                }
            }
            return true;
        }

        #region private

        private void PegasusConfig(Dictionary<string, string> inputs, out List<TpCurrencyConfig> currencyConfigList)
        {
            var currencyData = _commonBll.GetTpCurrencyList((int)Tp.Pegasus);
            currencyConfigList = new List<TpCurrencyConfig>();

            foreach (var tpCurrencyId in currencyData)
            {
                var currencyConfig = JsonConvert.DeserializeObject<TpCurrencyConfig>(inputs[$"Currency_{tpCurrencyId.TpCurrencyId}"]);
                if (currencyConfig is not null)
                {
                    currencyConfig.Currency = tpCurrencyId.TpCurrencyId;
                    currencyConfigList.Add(currencyConfig);
                }
            }
        }

        #endregion
    }
}
