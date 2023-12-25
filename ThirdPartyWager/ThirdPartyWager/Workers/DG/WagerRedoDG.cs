using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerRedoDG : BackgroundService
    {
        private Timer _timer;
        private readonly ILogger<WagerRedoDG> _logger;
        private readonly WagerRedoDGBLL _wagerRedoDGBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _intervalMins = 60;
        private readonly int _tpId = (int)Tp.DG;
        private readonly string _tpName = Tp.DG.ToString();
        private readonly int _tpType = (int)WagerTimerType.RedoWager;
        private readonly int _timeZone = 8;

        public WagerRedoDG(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerRedoDG>>();
            _wagerRedoDGBLL = provider.GetRequiredService<WagerRedoDGBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTask(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            int quick = 0;

            try
            {
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, _tpType);
                int last = _intervalMins;
                DateTime timerTime = (dataTpTimer == null) ? DateTime.Now.AddMinutes(-2 * _intervalMins) : dataTpTimer.StartTime.AddSeconds(1);

                var time = _dateTimeHelper.GetTimeByHour(last, _timeZone, "yyyy-MM-dd HH:mm:ss", timerTime);
                var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
                if (time.endUnix < cvTime)
                {
                    var result = await _wagerRedoDGBLL.RedoWagersAsync(_tpId, time.startTime, time.endTime, time.startUnix, time.endUnix);
                    if (result)
                    {
                        _thirdPartyBLL.SetThirdPartyTimer(_tpId, _tpType, time.endUnix);
                    }
                }
                var ts = DateTime.Now - timerTime;
                var diffMin = (int)ts.TotalMinutes;
                if (diffMin >= 120)
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
                var dueTimeSpan = (quick == 1) ? TimeSpan.FromMinutes(15) : TimeSpan.FromMinutes(_intervalMins);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
