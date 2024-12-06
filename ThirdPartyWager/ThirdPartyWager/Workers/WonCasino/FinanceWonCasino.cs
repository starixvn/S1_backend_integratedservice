using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class FinanceWonCasino : BackgroundService
    {
        private readonly ILogger<FinanceWonCasino> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly FinanceWonCasinoBLL _financeWonCasinoBLL;
        private readonly FinanceService _financeService;
        private readonly int _tpId = (int)Tp.WonCasino;
        private readonly string _tpName = Tp.WonCasino.ToString();
        private readonly int _tpType = (int)WagerTimerType.Finance;
        private readonly int _beyond = 2;
        private Timer timer;

        public FinanceWonCasino(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceWonCasino>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _financeWonCasinoBLL = provider.GetRequiredService<FinanceWonCasinoBLL>();
            _financeService = provider.GetRequiredService<FinanceService>();
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
                if (quick == 1)
                {
                    var dueTimeSpan = TimeSpan.FromHours(1);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromHours(4);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        private async Task<bool> ExecReportAsync()
        {
            var quick = false;
            try
            {
                // get timer
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, _tpType);

                if (dataTpTimer == null)
                {
                    dataTpTimer = new ThirdPartyTimerModel()
                    {
                        TpId = _tpId,
                        Type = _tpType,
                        StartTime = DateTime.Now.AddDays(-5),
                    };
                }
                else
                {
                    dataTpTimer.StartTime = dataTpTimer.StartTime.AddSeconds(1);
                }

                // get time 
                var day = 0;
                var reportTime = _financeWonCasinoBLL.GetReportTime(dataTpTimer.StartTime, day);

                // report
                var reportDate = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddSeconds(reportTime.endUnix);
                var reportData = await _financeWonCasinoBLL.GetReport(_tpId, null, reportTime);
                int result;
                if (reportData.Count > 0)
                {
                    // process
                    var dailyData = _financeWonCasinoBLL.DealReport(_tpId, reportDate, reportData);

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
    }
}
