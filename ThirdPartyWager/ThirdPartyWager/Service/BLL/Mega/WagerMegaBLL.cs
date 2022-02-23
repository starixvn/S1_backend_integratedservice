using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Library;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerMegaBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly WagerMegaService _wagerMegaService;
        private readonly ApiMegaBLL _apiMegaBLL;
        private readonly ILogger<WagerMegaBLL> _logger;
        private readonly DateTimeHelper _dateTimeHelper;

        private readonly int tpId = (int)Tp.Mega;
        private readonly string timeFormat = "yyyy-MM-dd HH:mm:ss";

        public WagerMegaBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerMegaBLL>>();
            _wagerMegaService = provider.GetRequiredService<WagerMegaService>();
            _apiMegaBLL = provider.GetRequiredService<ApiMegaBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        }

        public async Task<bool> ExecReportPlayerListWorker(int type, string currency, int timeRange)
        {
            // redo
            var deduct = (type == 2) ? -3 : -1;
            var excess = (type == 2) ? 3 : 1;

            // 結帳時間
            var wokerTime = GetWorkerTime(type, currency, deduct, -5);
            bool quick = CheckQuick(wokerTime, excess);
            
            // 依結帳時間取得前一天撈取時間範圍 (Ex. wokerTime = 2022-02-19 06:00:00，撈取時間範圍為 2022-02-18 00:00:00 ~ 2022-02-18 23:59:59)
            TimeCast time = _dateTimeHelper.GetTimeByDay(timeRange, 0, timeFormat, wokerTime.AddDays(-1));

            bool result = await GetReportPlayerListAsync(time, currency);
            if (result)
            {
                // 取得下個結帳時間
                var nextSettlementTime = ((DateTimeOffset)wokerTime.AddDays(1)).ToUnixTimeSeconds();

                // 存下個結帳時間
                _thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, type, nextSettlementTime, currency);
            }
            else
            {
                quick = true;
            }

            return quick;
        }

        private async Task<bool> GetReportPlayerListAsync(TimeCast time, string currency)
        {
            var msg = "FATAL_ERROR";

            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

                // get report player
                var dataReportPlayer = await _apiMegaBLL.GetReportPlayerListByTime(setting, currency, time.startTime, time.endTime);

                if (dataReportPlayer.code != null)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataReportPlayer)}");
                    return false;
                }

                if (!dataReportPlayer.result.Any())
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataReportPlayer)}");
                    return true;
                }

                foreach (var reportList in dataReportPlayer.result)
                {
                    try
                    {
                        var st = time.startUnix - 28800;
                        var ed = time.endUnix - 28800;
                        reportList.TransId = $"{st}_{reportList.loginId}";
                        reportList.Currency = currency;
                        reportList.StartUnix = st;
                        reportList.EndUnix = ed;
                        var r = _wagerMegaService.InsertReport(reportList).Result;
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_REPORT(PLAYER: {reportList.loginId} )";
                        _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(reportList)}\r\n{ex.Message}");
                        return false;
                    }
                }

                msg = "FINISH";
                _logger.LogInformation($"GetWagersAsync {msg}\r\n{currency} {time.startTime} ~ {time.endTime}");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"GetWagersAsync {msg}\r\n{ex.Message}");
                return false;
            }
        }

        private DateTime GetWorkerTime(int type, string currency, int vary, int begin)
        {
            var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, type, currency);
            var initTime = DateTime.UtcNow;
            var current = new DateTime(initTime.Year, initTime.Month, initTime.Day, 6, 0, 0, DateTimeKind.Utc);

            if (lastTime == null)
            {
                return current.AddDays(begin);
            }
            else
            {
                return (lastTime.StartTime > initTime) ? current.AddDays(vary) : lastTime.StartTime;
            }
        }

        private bool CheckQuick(DateTime workTime, int excess)
        {
            var diff = DateTime.UtcNow - workTime;
            return DateTime.UtcNow > workTime && diff.Days > excess;
        }
    }
}
