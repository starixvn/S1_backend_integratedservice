using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerWM : BackgroundService
    {
        private Timer _timer;
        private readonly ILogger<WagerWM> _logger;
        private readonly WagerWMBLL _wagerWMBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _intervalMins = 3;
        private readonly int _iRangMins = 20;
        private readonly int _tpId = (int)Tp.WM;
        private readonly string _tpName = Tp.WM.ToString();
        private readonly int _tpType = (int)WagerTimerType.Wager;
        private int _quick = 0;
        private readonly int _timeZone = 8;
        private readonly string _timeFormat = "yyyyMMddHHmmss";

        public WagerWM(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerWM>>();
            _wagerWMBLL = provider.GetRequiredService<WagerWMBLL>();
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
                int last = _iRangMins - 1;
                DateTime timerTime = (dataTpTimer == null) ? DateTime.Now.AddMinutes(-1 * last) : dataTpTimer.StartTime;

                var time = _dateTimeHelper.GetTimeByMinute(last, _timeZone, _timeFormat, timerTime);
                var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
                if (time.endUnix < cvTime)
                {
                    var result = await _wagerWMBLL.GetWagersAsync(time.startTime, time.endTime);
                    if (result)
                    {
                        _thirdPartyBLL.SetThirdPartyTimer(_tpId, _tpType, time.endUnix);
                    }
                }

                var ts = DateTime.Now - timerTime;
                var diffMin = (int)ts.TotalMinutes;
                if (diffMin > 5)
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
                var dueTimeSpan = (_quick == 1) ? TimeSpan.FromSeconds(90) : TimeSpan.FromMinutes(_intervalMins);
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
