using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL.HL
{
    public class WagerHLBLL
    {
        private readonly ApiHLBLL _apiHLBLL;
        private readonly WagerHLService _wagerHLService;
        private readonly ILogger<WagerHLBLL> _logger;
        private readonly int _tpId = (int)Tp.HaoLong;
        private readonly HLConfig _siteHLConfig;

        public WagerHLBLL(IServiceProvider provider)
        {
            _apiHLBLL = provider.GetRequiredService<ApiHLBLL>();
            _wagerHLService = provider.GetRequiredService<WagerHLService>();
            _logger = provider.GetRequiredService<ILogger<WagerHLBLL>>();
            _siteHLConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<HLConfig>(Tp.Allbet.ToString());
        }

        public async Task<bool> GetWagersAsync(string startTime, string endTime)
        {
            var msg = "FATAL_ERROR";
            var guid = Guid.NewGuid().ToString();
            try
            {
                var page = 1;

                var result = _apiHLBLL.GetBetListByTime(startTime,endTime,guid,page);
                var dataWager = result.Result.hl_Response;

                if (dataWager.ErrorCode != 0 && dataWager.ErrorCode != 998)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return false;
                }
                
                if (dataWager.ErrorCode == 0 && dataWager.Desc == "Data not Found")
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return true;
                }

                //繁忙時間 就直接回傳了
                if (dataWager.ErrorCode == 998)
                {
                    msg = "Busy Time";
                    _logger.LogInformation($"GetWagersAsync {msg}\r\n{startTime} ~ {endTime}");
                    return true;
                }
                
                foreach (var betData in dataWager.BetData)
                {
                    try
                    {
                        _ = _wagerHLService.InsertHLWager(betData);
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(WAGERID: {betData.BetID} )";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(betData)}\r\n{ex.Message}");
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