using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Workers
{
    public class FinanceSA : BackgroundService
    {
        private Timer _timer;
        private readonly ILogger<FinanceSA> _logger;
        private readonly FinanceSABLL _financeSABLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly FincanceService _fincanceService;
        private readonly int _tpId = (int)Tp.SA;
        private readonly string _tpName = Tp.SA.ToString();
        private readonly int _tpType = (int)WagerTimerType.Finance;
        private readonly int _beyond = 2;

        public FinanceSA(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceSA>>();
            _financeSABLL = provider.GetRequiredService<FinanceSABLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _fincanceService = provider.GetRequiredService<FincanceService>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoReport, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoReport(object state)
        {
            var quick = 0;
            try
            {
                LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
                LogContext.PushProperty("Tp", _tpName);
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
                var dueTimeSpan = (quick == 1) ? TimeSpan.FromHours(1) : TimeSpan.FromHours(4);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }


        private async Task<bool> ExecReportAsync()
        {
            var quick = false;
            try
            {
                // get setting
                var setting = string.Empty;

                // get timer
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, _tpType);

                // default time
                var execTime = new ThirdPartyTimerModel()
                {
                    TpId = _tpId,
                    Type = _tpType,
                    StartTime = (dataTpTimer == null) ? DateTime.Now.AddDays(-4) : dataTpTimer.StartTime.AddSeconds(1),
                };

                // get time 
                var day = 0;
                TimeCast reportTime = _financeSABLL.GetReportTime(execTime.StartTime, day);

                // report
                DateTime reportDate = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddSeconds(reportTime.endUnix);
                var reportData = await _financeSABLL.GetReport(_tpId, setting, reportTime);
                int result;
                if (reportData.Count > 0)
                {
                    // process
                    var dailyData = _financeSABLL.DealReport(_tpId, reportDate, reportData);

                    // write
                    result = _fincanceService.SetFinanceTpDailySummary(dailyData);
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
                if (diffDay > _beyond)
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
