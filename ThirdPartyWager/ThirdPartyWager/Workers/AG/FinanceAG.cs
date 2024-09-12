using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class FinanceAG : BackgroundService
    {
        private readonly IHostEnvironment _environment;
        private readonly ILogger<FinanceAG> _logger;
        private readonly FinanceAGBLL _financeAGBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly FincanceService _fincanceService;
        private readonly int _tpId = (int)Tp.AG;
        private readonly string _tpName = Tp.AG.ToString();
        private readonly int _tpType = (int)WagerTimerType.Finance;
        private readonly int _beyond = 2;
        private Timer _timer;

        public FinanceAG(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceAG>>();
            _financeAGBLL = provider.GetRequiredService<FinanceAGBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _fincanceService = provider.GetRequiredService<FincanceService>();
            _environment = provider.GetRequiredService<IHostEnvironment>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoReport, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoReport(object state)
        {
            if (!_environment.IsProduction())
            {
                return;
            }

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
                    StartTime = (dataTpTimer == null) ? DateTime.UtcNow.AddDays(-2) : dataTpTimer.StartTime.AddSeconds(1),
                };

                // get time 
                var day = 0;
                TimeCast reportTime = _financeAGBLL.GetReportTime(execTime.StartTime, day);

                // report
                DateTime reportDate = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddSeconds(reportTime.endUnix);
                var reportData = await _financeAGBLL.GetReport(_tpId, setting, reportTime);
                int result;
                if (reportData.Count > 0)
                {
                    // process
                    var dailyData = _financeAGBLL.DealReport(_tpId, reportDate, reportData);

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
