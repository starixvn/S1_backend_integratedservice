using Serilog.Context;

using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers.FaChai
{
    public class FinanceFaChai : BackgroundService
    {
        private readonly ILogger<FinanceFaChai> _logger;
        private readonly FinanceFaChaiBLL _financeFaChaiBBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly FincanceService _fincanceService;
        private readonly IConfiguration _configuration;
        private readonly int _tpId = (int)Tp.FaChai;
        private readonly string _tpName = Tp.FaChai.ToString();
        private readonly int _tpType = (int)WagerTimerType.Finance;
        private readonly int _beyond = 2;
        private Timer timer;

        public FinanceFaChai(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceFaChai>>();
            _financeFaChaiBBLL = provider.GetRequiredService<FinanceFaChaiBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _fincanceService = provider.GetRequiredService<FincanceService>();
            _configuration = provider.GetRequiredService<IConfiguration>();
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
                var interval = quick == 1
                    ? _configuration.GetValue<int>("FinanceJobInterval:WhenError")!
                    : _configuration.GetValue<int>("FinanceJobInterval:WhenSuccess")!;
                var dueTimeSpan = TimeSpan.FromMinutes(interval);
                _logger.LogInformation("{TpName}: update interval to {Interval} ", _tpName, interval);
                timer.Change(dueTimeSpan, TimeSpan.Zero);
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
                TimeCast reportTime = _financeFaChaiBBLL.GetReportTime(execTime.StartTime, day);

                // report
                DateTime reportDate = DateTime.UnixEpoch.AddSeconds(reportTime.endUnix);
                var reportData = await _financeFaChaiBBLL.GetReport(_tpId, null, reportTime);
               
                if (reportData.Any())
                {
                    // process
                    var dailyData = _financeFaChaiBBLL.DealReport(_tpId, reportDate, reportData);

                    // write
                   _fincanceService.SetFinanceTpDailySummary(dailyData);
                }

                // update timer
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

