using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerRedoWM : BackgroundService
    {
        private Timer _timer;
        private readonly ILogger<WagerRedoWM> _logger;
        private readonly WagerRedoWMBLL _wagerRedoWMBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _intervalMins = 60;
        private readonly int _tpId = (int)Tp.WM;
        private readonly string _tpName = Tp.WM.ToString();
        private readonly int _tpType = (int)WagerTimerType.RedoWager;
        private int _quick = 0;
        private readonly int _timeZone = 8;
        private readonly string _timeFormat = "yyyyMMddHHmmss";

        public WagerRedoWM(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerRedoWM>>();
            _wagerRedoWMBLL = provider.GetRequiredService<WagerRedoWMBLL>();
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

            try
            {
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, _tpType);
                int last = _intervalMins;
                DateTime timerTime = (dataTpTimer == null) ? DateTime.Now.AddMinutes(-2 * _intervalMins) : dataTpTimer.StartTime.AddSeconds(1);

                var time = _dateTimeHelper.GetTimeByHour(last, _timeZone, _timeFormat, timerTime);
                var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
                if (time.endUnix < cvTime)
                {
                    var result = await _wagerRedoWMBLL.RedoWagersAsync(time.startTime, time.endTime, time.startUnix, time.endUnix);
                    if (result)
                    {
                        _thirdPartyBLL.SetThirdPartyTimer(_tpId, _tpType, time.endUnix);
                    }
                }

                var ts = DateTime.Now - timerTime;
                var diffMin = (int)ts.TotalMinutes;
                if (diffMin >= 120)
                {
                    _quick = 1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = (_quick == 1) ? TimeSpan.FromSeconds(120) : TimeSpan.FromMinutes(28);
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