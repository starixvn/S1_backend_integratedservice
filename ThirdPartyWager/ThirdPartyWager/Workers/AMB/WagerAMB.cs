using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Workers;

public class WagerAMB : BackgroundService
{
    private readonly int _tpId = (int)Tp.AMB;
        private readonly string _tpName = Tp.AMB.ToString();
        private Timer _timer;
        private readonly WagerAMBBLL _wagerAMBBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ILogger<WagerGA> _logger;
        private readonly int timeZone = 8;
        private readonly int iRangMins = 5;
        private readonly int tpType = (int)WagerTimerType.Wager;
        private int quick = 0;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";
        public WagerAMB(IServiceProvider provider)
        {
            _wagerAMBBLL = provider.GetRequiredService<WagerAMBBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _logger = provider.GetRequiredService<ILogger<WagerGA>>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoAMBReportTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoAMBReportTask(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            try
            {
                var nowTime = DateTime.Now;
                _logger.LogInformation($"DoAMBReport Start DoAMBReportTask Job Time : : +0 {nowTime:yyyy-MM-ddTHH:mm:ss}");

                var result = false;
                quick = 0;
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, tpType);
                
                var timerTime = dataTpTimer?.StartTime ?? nowTime.AddMinutes(-(iRangMins - 1));
                var last = (dataTpTimer == null) ? iRangMins - 1 : iRangMins;
                var time = _dateTimeHelper.GetTimeByMinute(last, timeZone, _timeFormat, timerTime);

                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
                result = await _wagerAMBBLL.GetWagerAsync(time.startTime, time.endTime);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimer(_tpId, tpType, time.endUnix);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(5);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    
}