using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerIconicBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiIconicBLL _apiIconicBLL;
        private readonly WagerIconicService _wagerIconicService;
        private readonly ILogger<WagerIconicBLL> _logger;

        private readonly int _tpId = (int)Tp.Iconic;
        private readonly string _tpName = Tp.Iconic.ToString();

        public WagerIconicBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiIconicBLL = provider.GetRequiredService<ApiIconicBLL>();
            _wagerIconicService = provider.GetRequiredService<WagerIconicService>();
            _logger = provider.GetRequiredService<ILogger<WagerIconicBLL>>();
        }

        public async Task<bool> GetWagersAsync(int tpId, long startUnix, long endUnix, string currency)
        {
            string msg = "FATAL_ERROR";
            string guid = Guid.NewGuid().ToString();
            LogContext.PushProperty("Tp", _tpName);
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

                int perPage = 200;
                int page = 1;
                int totalPage = 1;
                while (page <= totalPage)
                {
                    // get wager
                    var dataWager = await _apiIconicBLL.GetBetListByTime(setting["ApiUrl"], setting[$"token_{currency}"], startUnix, endUnix, page, perPage, guid);

                    // error condition
                    if (dataWager.error != null)
                    {
                        var errorCode = dataWager.error.status;
                        if (errorCode != 0)
                        {
                            msg = "THIRD_PARTY_ERROR";
                            _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                            return false;
                        }
                    }

                    if (dataWager.totalSize == 0)
                    {
                        msg = "NO_DATA";
                        _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return true;
                    }

                    totalPage = (int)((dataWager.totalSize / perPage) + 1);
                    foreach (var bet in dataWager.data)
                    {
                        try
                        {
                            if (bet.gameType != "fish")
                            {
                                continue;
                            }
                            if (bet.status == "playing")
                            {
                                continue;
                            }
                            _wagerIconicService.InsertWager(bet);
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {bet.id} )";
                            _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                            return false;
                        }
                    }
                    page++;
                }

                msg = "FINISH";
                _logger.LogInformation($"GetWagersAsync {msg}\r\n{startUnix} ~ {endUnix}");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError($"GetWagersAsync {msg}\r\n{ex.Message}");
                return false;
            }
        }
    }
}
