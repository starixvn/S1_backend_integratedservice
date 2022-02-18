using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerWMBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiWMBLL _apiWMBLL;
        private readonly WagerWMService _wagerWMService;
        private readonly ILogger<WagerWMBLL> _logger;
        private readonly IHostEnvironment _environment;

        private readonly int _tpId = (int)Tp.WM;

        public WagerWMBLL(IServiceProvider provider, IHostEnvironment environment)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiWMBLL = provider.GetRequiredService<ApiWMBLL>();
            _wagerWMService = provider.GetRequiredService<WagerWMService>();
            _logger = provider.GetRequiredService<ILogger<WagerWMBLL>>();
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

                var currencyList = new List<string> { "RMB" };
                if (_environment.IsProduction())
                {
                    currencyList.Add("THB");
                    currencyList.Add("MYR");
                    currencyList.Add("IDRK");
                    currencyList.Add("VNDK");
                    currencyList.Add("USD");
                    currencyList.Add("INR");
                }

                foreach (var curr in currencyList)
                {
                    // get wager
                    var dataWager = await _apiWMBLL.GetBetListByTime(setting["ApiUrl"], setting[$"vendorId_{curr}"], setting[$"signature_{curr}"], startTime, endTime, guid);

                    // error condition
                    var errorCode = dataWager.errorCode;
                    var errorMsg = dataWager.errorMessage;

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
                            bet.Currency = curr;
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
