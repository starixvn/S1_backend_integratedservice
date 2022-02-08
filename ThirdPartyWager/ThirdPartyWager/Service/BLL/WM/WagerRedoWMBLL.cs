using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerRedoWMBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiWMBLL _apiWMBLL;
        private readonly WagerWMService _wagerWMService;
        private readonly ILogger<WagerRedoWMBLL> _logger;
        private readonly IHostEnvironment _environment;

        private readonly int _tpId = (int)Tp.WM;

        public WagerRedoWMBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiWMBLL = provider.GetRequiredService<ApiWMBLL>();
            _wagerWMService = provider.GetRequiredService<WagerWMService>();
            _logger = provider.GetRequiredService<ILogger<WagerRedoWMBLL>>();
            _environment = provider.GetRequiredService<IHostEnvironment>();
        }

        public async Task<bool> RedoWagersAsync(int tpId, string startTime, string endTime, long startUnix, long endUnix)
        {
            string msg = "FATAL_ERROR";
            string guid = Guid.NewGuid().ToString();
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

                var currencyList = new List<string>();
                currencyList.Add("RMB");
                if (_environment.IsProduction())
                {
                    currencyList.Add("THB");
                    currencyList.Add("MYR");
                    currencyList.Add("IDRK");
                    currencyList.Add("VNDK");
                    currencyList.Add("USD");
                    currencyList.Add("INR");
                }
                bool reCheck = false;
                foreach (var curr in currencyList)
                {
                    // count interval wagers
                    var getWagerCount = _wagerWMService.GetWagerTotal(startUnix, endUnix, curr);
                    int wagerCount = getWagerCount.wagerCount;

                    var dataWager = await _apiWMBLL.GetBetListByTime(setting["ApiUrl"], setting[$"vendorId_{curr}"], setting[$"signature_{curr}"], startTime, endTime, guid);

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
