using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Workers
{
    public class FinanceYGG : BackgroundService
    {
        private readonly ILogger<FinanceYGG> _logger;
        private readonly FinanceYGGBLL _financYGGBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly FinanceService _financeService;
        private readonly YGGConfig _siteConfig;
        private readonly int _tpId = (int)Tp.YGG;
        private readonly string _tpName = Tp.YGG.ToString();
        private readonly int _tpType = (int)WagerTimerType.Finance;
        private readonly int _beyond = 2;
        private Timer _timer;

        public FinanceYGG(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceYGG>>();
            _financYGGBLL = provider.GetRequiredService<FinanceYGGBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _financeService = provider.GetRequiredService<FinanceService>();
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<YGGConfig>(_tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoReport, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoReport(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var quick = 0;
            try
            {
                var res = await ExecReportAsync();
                if (res)
                {
                    quick = 1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                if (quick == 1)
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(10);
                    _timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromHours(4);
                    _timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }


        private async Task<bool> ExecReportAsync()
        {
            var quick = false;
            try
            {
                // get setting
                var setting = _siteConfig;

                // get timer
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, _tpType);

                // default time
                var execTime = new ThirdPartyTimerModel()
                {
                    TpId = _tpId,
                    Type = _tpType,
                    StartTime = (dataTpTimer == null) ? DateTime.Now.AddDays(-2) : dataTpTimer.StartTime.AddSeconds(1),
                };

                // get time 
                var day = 0;
                TimeCast reportTime = _financYGGBLL.GetReportTime(execTime.StartTime, day);

                // report
                DateTime reportDate = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddSeconds(reportTime.endUnix);
                var reportData = await _financYGGBLL.GetReport(_tpId, setting, reportTime);
                int result;
                if (reportData.Count > 0)
                {
                    // process
                    var dailyData = _financYGGBLL.DealReport(_tpId, reportDate, reportData);

                    // write
                    result = _financeService.SetFinanceTpDailySummary(dailyData);
                }
                else
                {
                    result = 1;
                }

                // update timer
                if (result > 0)
                {
                    _thirdPartyBLL.SetThirdPartyTimer(_tpId, _tpType, reportTime.endUnix);
                }

                var diffDay = (int)(DateTime.Now.Date - reportDate.Date).TotalDays;
                if (diffDay >= _beyond)
                {
                    quick = true;
                }

                _logger.LogInformation($"{_tpName} Get {reportDate:yyyy-MM-dd} report");
                return quick;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                quick = true;
            }

            _logger.LogInformation($"Report Worker Execute End {_tpId}");
            return quick;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}

