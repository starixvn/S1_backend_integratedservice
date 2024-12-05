using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.BLL.Live22;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Workers.Live22
{
    public class FinanceLive22 : BackgroundService
    {
        private readonly ILogger<FinanceLive22> _logger;
        private readonly FinanceLive22BLL _financeLive22Bll;
        private Timer _timer;
        private readonly string _tpName = Tp.Live22.ToString();
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly FincanceService _financeService;
        private readonly int _tpId = (int)Tp.Live22;
        private readonly int _tpType = (int)WagerTimerType.Finance;
        private readonly int _beyond = 2;

        public FinanceLive22(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceLive22>>();
            _financeLive22Bll = provider.GetRequiredService<FinanceLive22BLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _financeService = provider.GetRequiredService<FincanceService>();
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
                //TODO
                // Los: Decrease interval time to test
                var dueTimeSpan = TimeSpan.FromMinutes(4);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private async Task<bool> ExecReportAsync()
        {
            var quick = false;

            try
            {
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, _tpType);

                var execTime = new ThirdPartyTimerModel()
                {
                    TpId = _tpId,
                    Type = _tpType,
                    StartTime = (dataTpTimer == null) ? DateTime.UtcNow.AddDays(-3) : dataTpTimer.StartTime.AddSeconds(1),
                };

                var day = 0;
                TimeCast reportTime = _financeLive22Bll.GetReportTime(execTime.StartTime, day);

                DateTime reportDate =
                    DateTime.UnixEpoch
                    .AddSeconds(reportTime.endUnix);

                var reportData = await _financeLive22Bll.GetReport(_tpId, null, reportTime);

              
                if (reportData.Count > 0)
                {
                    // compare
                    var dailyData = _financeLive22Bll.DealReport(_tpId, reportDate, reportData);

                    // write to current report
                    _financeService.SetFinanceTpDailySummary(dailyData);
                }

                // update timer
                
                    // set timer to end time, which is the next day ???
                    _thirdPartyBLL.SetThirdPartyTimer(_tpId, _tpType, reportTime.endUnix);
                

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

            _logger.LogInformation($"Report Worker Execute End {_tpName}");
            return quick;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
