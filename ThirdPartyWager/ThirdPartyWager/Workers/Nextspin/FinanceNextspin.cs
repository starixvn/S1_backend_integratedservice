using Serilog.Context;

using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class FinanceNextspin : BackgroundService
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly INextspinFinancBLL _financBLL;
        private readonly ILogger<FinanceNextspin> _logger;
        private const int TpId = (int)Tp.Nextspin;
        private readonly int _tpType = (int)WagerTimerType.Finance;
        private readonly int _beyond = 2; //超過2天開加速
        private Timer timer;

        public FinanceNextspin(ThirdPartyBLL thirdPartyBLL, DateTimeHelper dateTimeHelper, INextspinFinancBLL financBLL, ILogger<FinanceNextspin> logger)
        {
            _thirdPartyBLL = thirdPartyBLL;
            _dateTimeHelper = dateTimeHelper;
            _financBLL = financBLL;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoReport, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoReport(object state)
        {
            var tpName = Tp.Nextspin.ToString();
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", tpName);
            var doQuickly = false;
            try
            {
                // get timer
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(TpId, _tpType);
                // 第一次執行,取兩天前資料
                if (dataTpTimer == null)
                {
                    dataTpTimer = new ThirdPartyTimerModel()
                    {
                        TpId = TpId,
                        Type = _tpType,
                        StartTime = DateTime.Now.AddDays(-2),
                    };
                }
                else
                    dataTpTimer.StartTime = dataTpTimer.StartTime.AddSeconds(1);

                // get time 
                var reportTime = _dateTimeHelper.GetTimeByDay(1440, 0, "yyyyMMddTHHmmss", dataTpTimer.StartTime);
                var cvTime = DateTimeOffset.Now.ToUnixTimeSeconds();
                if (reportTime.endUnix > cvTime)
                {
                    reportTime = _dateTimeHelper.GetTimeByDay(1440, 0, "yyyyMMddTHHmmss", dataTpTimer.StartTime.AddDays(-1));
                }

                await _financBLL.ExecReportAsync(TpId, _tpType, reportTime);

                var diffDay = (int)(DateTime.Now.Date - dataTpTimer.StartTime).TotalDays;
                if (diffDay >= _beyond)
                {
                    doQuickly = true;
                }
                _logger.LogInformation($"{tpName} Get {dataTpTimer.StartTime:yyyy-MM-dd} report");
            }
            catch (Exception ex)
            {
                doQuickly = true;
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var nextTimeSpan = doQuickly ? TimeSpan.FromMinutes(10) : TimeSpan.FromHours(4);
                timer.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
