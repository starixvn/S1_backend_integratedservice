using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;
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
        private readonly IHostEnvironment _environment;

        private readonly int tpId = (int)Tp.Mega;
        private readonly int timeZone = 8;
        private readonly string timeFormat = "yyyy-MM-dd HH:mm:ss";

        public WagerMegaBLL(IServiceProvider provider, IHostEnvironment environment)
        {
            _logger = provider.GetRequiredService<ILogger<WagerMegaBLL>>();
            _wagerMegaService = provider.GetRequiredService<WagerMegaService>();
            _apiMegaBLL = provider.GetRequiredService<ApiMegaBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _environment = environment;
        }

        public async Task<bool> ExecReportPlayerListWorker(int type, string currency, int timeRange)
        {
            var wokerTime = GetWorkerTime(type, currency, 1, -5);
            // 取得下個結帳時間
            var settlementTime = GetSettlementTime(wokerTime).AddDays(1);
            var nextSettlementTime = ((DateTimeOffset)settlementTime).ToUnixTimeSeconds() - 1;
            TimeCast time = _dateTimeHelper.GetTimeByDay(timeRange, timeZone, timeFormat, wokerTime);

            // redo
            var deduct = (type == 2) ? -2 : 0;
            var excess = (type == 2) ? 3 : 1;
            bool quick = CheckQuick(wokerTime, excess);
            var cvTime = DateTimeOffset.Now.AddDays(deduct).ToOffset(new TimeSpan(timeZone, 0, 0)).ToUnixTimeSeconds();

            // 檢查結帳時間
            var pending = cvTime < nextSettlementTime;
            if (!pending)
            {
                bool result = await GetReportPlayerListAsync(time, currency);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, type, nextSettlementTime, currency);
                }
                else
                {
                    quick = true;
                }
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
                        reportList.TransId = $"{time.startUnix}_{reportList.loginId}";
                        reportList.Currency = currency;
                        reportList.StartUnix = time.startUnix;
                        reportList.EndUnix = time.endUnix;
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
            return (lastTime == null) ? DateTime.Now.AddDays(begin) : lastTime.StartTime.AddSeconds(vary);
        }

        private bool CheckQuick(DateTime workTime, int excess)
        {
            var diff = DateTime.Now - workTime;
            return DateTime.Now > workTime && diff.Days > excess;
        }

        private DateTime GetSettlementTime(DateTime time)
        {
            var c = new DateTimeOffset(new DateTime(time.Year, time.Month, time.Day, 14, 0, 0), new TimeSpan(8, 0, 0));
            return c.DateTime;
        }
    }
}
