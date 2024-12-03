using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.BLL.FastSpin;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model;


namespace ThirdPartyWager.Workers.FastSpin
{
    public class FinanceFastSpin : BackgroundService
    {
        private readonly ILogger<FinanceFastSpin> _logger;
        private readonly FinanceFastSpinBLL _financeFastSpinBll;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly FincanceService _fincanceService;
        private readonly int _tpId = (int)Tp.FastSpin;
        private readonly string _tpName = Tp.FastSpin.ToString();
        private readonly int _tpType = (int)WagerTimerType.Finance;
        private readonly int _beyond = 2;
        private Timer timer;

        public FinanceFastSpin(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceFastSpin>>();
            _financeFastSpinBll = provider.GetRequiredService<FinanceFastSpinBLL>();
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
                //TODO
                // Los: Decrease interval time to test
                var dueTimeSpan = TimeSpan.FromMinutes(4);
                timer.Change(dueTimeSpan, TimeSpan.Zero);
                //if (quick == 1)
                //{
                //    var dueTimeSpan = TimeSpan.FromHours(1);
                //    timer.Change(dueTimeSpan, TimeSpan.Zero);
                //}
                //else
                //{
                //    var dueTimeSpan = TimeSpan.FromHours(4);
                //    timer.Change(dueTimeSpan, TimeSpan.Zero);
                //}
            }
        }


        private async Task<bool> ExecReportAsync()
        {
            var quick = false;
            try
            {
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
                TimeCast reportTime = _financeFastSpinBll.GetReportTime(execTime.StartTime, day);

                // report
                DateTime reportDate = DateTime.UnixEpoch.AddSeconds(reportTime.endUnix);
                var reportData = await _financeFastSpinBll.GetReport(_tpId, null, reportTime);
                int result;
                if (reportData.Any())
                {
                    // process
                    var dailyData = _financeFastSpinBll.DealReport(_tpId, reportDate, reportData);

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

            _logger.LogInformation("Report worker execute end {TpName}", _tpName);
            return quick;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
