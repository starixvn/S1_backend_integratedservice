using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class FinancePlayTech : BackgroundService
    {
        private readonly ILogger<FinancePlayTech> _logger;
        private readonly FinancePlayTechBLL _financPlayTechBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly FincanceService _fincanceService;
        private readonly int _tpId = (int)Tp.PlayTech;
        private readonly string _tpName = Tp.PlayTech.ToString();
        private readonly int _tpType = (int)WagerTimerType.Finance;
        private readonly int _beyond = 2;
        private Timer timer;

        public FinancePlayTech(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinancePlayTech>>();
            _financPlayTechBLL = provider.GetRequiredService<FinancePlayTechBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _fincanceService = provider.GetRequiredService<FincanceService>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoReport, null, TimeSpan.Zero, TimeSpan.Zero);
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
                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private async Task<bool> ExecReportAsync()
        {
            var quick = false;
            try
            {
                // get setting
                var setting = "";//改由下一層取全部設定

                // get timer
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, _tpType);

                // default time
                var execTime = new ThirdPartyTimerModel()
                {
                    TpId = _tpId,
                    Type = _tpType,
                    StartTime = (dataTpTimer == null) ? DateTime.UtcNow.AddDays(-2) : dataTpTimer.StartTime,
                };

                // get time 
                var day = 0;
                TimeCast reportTime = _financPlayTechBLL.GetReportTime(execTime.StartTime, day);

                // report
                DateTime reportDate = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddSeconds(reportTime.startUnix);
                var reportData = await _financPlayTechBLL.GetReport(_tpId, setting, reportTime);
                int result;
                if (reportData.Count > 0)
                {
                    // process
                    var dailyData = _financPlayTechBLL.DealReport(_tpId, reportDate, reportData);

                    if (dailyData.Count() > 0)
                    {
                        // write
                        result = _fincanceService.SetFinanceTpDailySummary(dailyData);
                    }
                    else
                    {
                        result = 1;
                    }
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
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
