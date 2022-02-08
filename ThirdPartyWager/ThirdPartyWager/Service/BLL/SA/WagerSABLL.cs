using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerSABLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiSABLL _apiSABLL;
        private readonly WagerSAService _wagerSAService;
        private readonly ILogger<WagerSABLL> _logger;

        private readonly int _tpId = (int)Tp.SA;

        public WagerSABLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiSABLL = provider.GetRequiredService<ApiSABLL>();
            _wagerSAService = provider.GetRequiredService<WagerSAService>();
            _logger = provider.GetRequiredService<ILogger<WagerSABLL>>();
        }

        public async Task<bool> GetWagersAsync(int tpId, string startTime, string endTime)
        {
            string msg = "FATAL_ERROR";
            string guid = Guid.NewGuid().ToString();
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

                // get wager
                var dataWager = await _apiSABLL.GetBetListByTime(setting["WagerUrl"], setting["EncrypKey"], setting["MD5Key"], setting["SecretKey"], startTime, endTime, guid);

                // error condition
                var errorCode = dataWager.GetAllBetDetailsForTimeIntervalResponse != null ? dataWager.GetAllBetDetailsForTimeIntervalResponse.ErrorMsgId : dataWager.APIResponse.ErrorMsgId;
                if (errorCode != 0)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return false;
                }
                if (errorCode == 0 && dataWager.GetAllBetDetailsForTimeIntervalResponse.BetDetailList == null)
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return true;
                }

                // insert wager to db
                foreach (var bet in dataWager.GetAllBetDetailsForTimeIntervalResponse.BetDetailList.BetDetail)
                {
                    try
                    {
                        _wagerSAService.InsertWager(bet);
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(WAGERID: {bet.BetID} )";
                        _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
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
                _logger.LogError(ex, $"GetWagersAsync {msg}\r\n{ex.Message}");
                return false;
            }
        }
    }
}
