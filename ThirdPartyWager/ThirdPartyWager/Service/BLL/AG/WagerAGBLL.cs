using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerAGBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiAGBLL _apiAGBLL;
        private readonly WagerAGService _wagerAGService;
        private readonly ILogger<WagerAGBLL> _logger;

        public WagerAGBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiAGBLL = provider.GetRequiredService<ApiAGBLL>();
            _wagerAGService = provider.GetRequiredService<WagerAGService>();
            _logger = provider.GetRequiredService<ILogger<WagerAGBLL>>();
        }

        public async Task<bool> GetWagersAsync(int tpId, string startTime, string endTime)
        {
            var msg = "FATAL_ERROR";
            var guid = Guid.NewGuid().ToString();
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

                // pagination
                var page = 1;
                var totalPage = 1;
                while (page <= totalPage)
                {
                    // get wager
                    var dataWager = await _apiAGBLL.GetBetListByTime(setting["WagerUrl"], setting["HashKey"], setting["data_cagent"], startTime, endTime, page, guid);

                    // error condition
                    var errorCode = dataWager.result.info;

                    if (errorCode != 0)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return false;
                    }

                    var totalNum = dataWager.result.addition.total;
                    totalPage = dataWager.result.addition.totalpage;
                    if (totalNum == 0)
                    {
                        msg = "NO_DATA";
                        _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return true;
                    }

                    foreach (var bet in dataWager.result.row)
                    {
                        try
                        {
                            _wagerAGService.InsertWager(bet);
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {bet.billNo} )";
                            _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                            return false;
                        }
                    }
                    page++;
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
