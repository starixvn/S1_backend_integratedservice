using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerOneGamingBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiOneGamingBLL _apiOneGamingBLL;
        private readonly WagerOneGamingService _wagerOneGamingService;
        private readonly ILogger<WagerOneGamingBLL> _logger;

        private readonly int _tpId = (int)Tp.OneGaming;
        private readonly string _tpName = Tp.OneGaming.ToString();

        public WagerOneGamingBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiOneGamingBLL = provider.GetRequiredService<ApiOneGamingBLL>();
            _wagerOneGamingService = provider.GetRequiredService<WagerOneGamingService>();
            _logger = provider.GetRequiredService<ILogger<WagerOneGamingBLL>>();
        }

        public async Task<bool> GetWagersAsync(int tpId, string startTime, string endTime, string currency)
        {
            string msg = "FATAL_ERROR";
            string guid = Guid.NewGuid().ToString();
            LogContext.PushProperty("Tp", _tpName);
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

                // get wager
                var dataWager = await _apiOneGamingBLL.GetBetListByTime(setting["ApiUrl"], setting[$"agent_code_{currency}"], setting[$"secret_key_{currency}"], startTime, endTime, guid);

                // error condition
                if (dataWager.status_code != "0")
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return false;
                }

                if (dataWager.data.Count == 0)
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return true;
                }

                foreach (var bet in dataWager.data)
                {
                    try
                    {
                        _wagerOneGamingService.InsertWager(bet);
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(WAGERID: {bet.ticket} )";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                        return false;
                    }
                }

                msg = "FINISH";
                _logger.LogInformation($"GetWagersAsync {msg}\r\n{startTime} ~ {endTime}");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError($"GetWagersAsync {msg}\r\n{ex.Message}");
                return false;
            }
        }

        //public WagerOneGamingTotal GetWagerTotal(long startUnix, long endUnix)
        //{
        //    return wagerOneGamingService.GetWagerTotal(startUnix, endUnix);
        //}
    }
}
