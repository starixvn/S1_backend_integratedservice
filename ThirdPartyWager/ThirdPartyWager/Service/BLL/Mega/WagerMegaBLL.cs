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

    public class WagerMegaBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiMegaBLL _apiMegaBLL;
        private readonly WagerMegaService _wagerMegaService;
        private readonly ILogger<WagerMegaBLL> _logger;

        private readonly int _tpId = (int)Tp.Mega;

        public WagerMegaBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiMegaBLL = provider.GetRequiredService<ApiMegaBLL>();
            _wagerMegaService = provider.GetRequiredService<WagerMegaService>();
            _logger = provider.GetRequiredService<ILogger<WagerMegaBLL>>();
        }

        public async Task<bool> GetWagersAsync(int tpId, string startTime, string endTime, string currency)
        {
            string msg = "FATAL_ERROR";
            string guid = Guid.NewGuid().ToString();
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

                // get report player
                var dataReportPlayer = await _apiMegaBLL.GetReportPlayerListByTime(setting["ApiUrl"], setting["sn"], setting[$"secretCode_{currency}"], setting[$"agentLoginId_{currency}"], startTime, endTime, guid);

                if (dataReportPlayer.code != null)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataReportPlayer)}");
                    return false;
                }

                if (/*dataReportPlayer.success == true &&*/ dataReportPlayer.result.Count() == 0)
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataReportPlayer)}");
                    return true;
                }

                foreach (var reportList in dataReportPlayer.result)
                {
                    try
                    {
                        reportList.win = reportList.win * -1;
                        reportList.Currency = currency;
                        reportList.WagerDate = Convert.ToDateTime(startTime);
                        _wagerMegaService.InsertWager(reportList);
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(WAGERID: {reportList.loginId} )";
                        _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(reportList)}\r\n{ex.Message}");
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
