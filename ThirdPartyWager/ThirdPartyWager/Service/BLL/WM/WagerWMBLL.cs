using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        public WagerWMBLL(IServiceProvider provider, IHostEnvironment environment)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiWMBLL = provider.GetRequiredService<ApiWMBLL>();
            _wagerWMService = provider.GetRequiredService<WagerWMService>();
            _logger = provider.GetRequiredService<ILogger<WagerWMBLL>>();
            _tpCurrency = provider.GetRequiredService<CommonBLL>().GetTpCurrencyList((int)Tp.WM);
            _environment = environment;
        }

        public async Task<bool> GetWagersAsync(int tpId, string startTime, string endTime)
        {
            var msg = "FATAL_ERROR";
            var guid = Guid.NewGuid().ToString();
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

                foreach (var currency in _tpCurrency)
                {
                    if (!_environment.IsProduction() && currency.TpCurrencyId != "RMB")
                    {
                        continue;
                    }
                    
                    // get wager
                    var dataWager = await _apiWMBLL.GetBetListByTime(setting["ApiUrl"], setting[$"vendorId_{currency.TpCurrencyId}"], setting[$"signature_{currency.TpCurrencyId}"], startTime, endTime, guid);

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
                            _wagerWMService.InsertWager(bet);
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
