using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerKiss918BLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiKiss918BLL _apiKiss918BLL;
        private readonly WagerKiss918Service _wagerKiss918Service;
        private readonly ILogger<WagerKiss918BLL> _logger;

        public WagerKiss918BLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiKiss918BLL = provider.GetRequiredService<ApiKiss918BLL>();
            _wagerKiss918Service = provider.GetRequiredService<WagerKiss918Service>();
            _logger = provider.GetRequiredService<ILogger<WagerKiss918BLL>>();
        }

        public async Task<bool> GetWagersAsync(int tpId, string startTime)
        {
            var msg = "FATAL_ERROR";
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

                //convert timepoint
                var timepoint = await _apiKiss918BLL.GetTimepoint(setting["ApiToken"], setting["ApiUrl"], startTime);

                if (string.IsNullOrWhiteSpace(timepoint))
                {
                    msg = "GET_TIMEPOINT_ERROR";
                    _logger.LogError($"{msg}\r\n{timepoint}");
                    return false;
                }

                // get history bets
                var (hasData, dataDetails) = await _apiKiss918BLL.GetPlaySession(setting["AccessToken"], setting["ApiUrl"], timepoint);

                if (!hasData)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataDetails)}");
                    return false;
                }

                if (hasData && dataDetails.Count() == 0)
                {
                    //msg = "NO_DATA";
                    //_logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(datadetails)}");
                    return true;
                }

                foreach (var data in dataDetails)
                {
                    try
                    {
                        _wagerKiss918Service.InsertWager(data);
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(WAGERID: {data.referenceID} )";
                        _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(data)}\r\n{ex.Message}");
                        return false;
                    }
                }
                msg = "FINISH";
                _logger.LogInformation($"GetWagersAsync {msg}\r\n{startTime} ~ {dataDetails.MaxBy(x => x.created).created}");
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
