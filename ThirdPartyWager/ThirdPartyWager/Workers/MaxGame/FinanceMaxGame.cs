using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.BLL.MaxGame;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Workers.MaxGame
{
    public class FinanceMaxGame : BackgroundService
    {
        private readonly ILogger<FinanceMaxGame> _logger;
        private readonly FinanceMaxGameBll _financeMaxGameBll;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly FinanceService _financeService;
        private readonly int _tpId = (int)Tp.MaxGame;
        private readonly string _tpName = Tp.MaxGame.ToString();
        private readonly int _tpType = (int)WagerTimerType.Finance;
        private readonly int _beyond = 2;
        private Timer timer;
        private readonly IConfiguration _configuration;

        public FinanceMaxGame(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceMaxGame>>();
            _financeMaxGameBll = provider.GetRequiredService<FinanceMaxGameBll>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _financeService = provider.GetRequiredService<FinanceService>();
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
                TimeCast reportTime = _financeMaxGameBll.GetReportTime(execTime.StartTime, day);

                // report
                DateTime reportDate = DateTime.UnixEpoch.AddSeconds(reportTime.endUnix);
                var reportData = await _financeMaxGameBll.GetReport(_tpId, null, reportTime);
               
                if (reportData.Any())
                {
                    // process
                    var dailyData = _financeMaxGameBll.DealReport(_tpId, reportDate, reportData);

                    // write
                    foreach (var data in dailyData)
                    {
                        // write
                        _financeService.SetFinanceTpDailySummary(data);
                    }
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
