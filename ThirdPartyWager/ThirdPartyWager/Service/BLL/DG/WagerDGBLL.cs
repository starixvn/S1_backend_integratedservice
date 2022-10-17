using Microsoft.Extensions.DependencyInjection;
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

    public class WagerDGBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiDGBLL _apiDGBLL;
        private readonly WagerDGService _wagerDGService;
        private readonly ILogger<WagerDGBLL> _logger;

        public WagerDGBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiDGBLL = provider.GetRequiredService<ApiDGBLL>();
            _wagerDGService = provider.GetRequiredService<WagerDGService>();
            _logger = provider.GetRequiredService<ILogger<WagerDGBLL>>();
        }

        public async Task<bool> GetWagersAsync()
        {
            var msg = "FATAL_ERROR";
            // get dg api info
            try
            {
                var setting = _thirdPartyBLL.GetThirdPartySetting((int)Tp.DG);

                var wagerIds = new List<long>();
                // get wagers
                var dataWager = await _apiDGBLL.GetBetList(setting["ApiUrl"], setting["AgentAccount"], setting["APIkey"]);

                var errorCode = dataWager.codeId;
                if (errorCode != 0)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return false;
                }

                if (dataWager.list == null || dataWager.list.Count() == 0)
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return true;
                }

                // insert wager to db
                foreach (var bet in dataWager.list)
                {
                    try
                    {
                        await _wagerDGService.InsertWagerDG(bet);
                        wagerIds.Add(bet.id);
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(WAGERID: {bet.id} )";
                        _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                        return false;
                    }
                }
                // mark wager
                await _apiDGBLL.MarkGotBetList(setting["ApiUrl"], setting["AgentAccount"], setting["APIkey"], wagerIds);

                msg = "FINISH";
                _logger.LogInformation($"GetWagersAsync {msg}");
                return dataWager.list.Count() == 1000;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"GetWagersAsync {msg}\r\n{ex.Message}");
                return true;
            }
        }
    }
}
