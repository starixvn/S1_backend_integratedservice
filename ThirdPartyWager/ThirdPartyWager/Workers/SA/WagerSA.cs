using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerSA : BackgroundService
    {
        private Timer _timer;
        private readonly ILogger<WagerSA> _logger;
        private readonly WagerSABLL _wagerSABLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _intervalMins = 3;
        private readonly int _iRangMins = 20;
        private readonly int _tpId = (int)Tp.SA;
        private readonly string _tpName = Tp.SA.ToString();
        private readonly int _tpType = (int)WagerTimerType.Wager;
        private readonly int _timeZone = 8;

        public WagerSA(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerSA>>();
            _wagerSABLL = provider.GetRequiredService<WagerSABLL>();
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
                int last = _iRangMins - 1;
                DateTime timerTime = (dataTpTimer == null) ? DateTime.Now.AddMinutes(-1 * last) : dataTpTimer.StartTime;

                if (timerTime < DateTime.Now)
                {
                    var time = _dateTimeHelper.GetTimeByMinute(last, _timeZone, "yyyy-MM-dd HH:mm:ss", timerTime);
                    var result = await _wagerSABLL.GetWagersAsync(_tpId, time.startTime, time.endTime);
                    if (result)
                    {
                        _thirdPartyBLL.SetThirdPartyTimer(_tpId, _tpType, time.endUnix);
                    }
                }
                var ts = DateTime.Now - timerTime;
                var diffMin = (int)ts.TotalMinutes;
                if (diffMin > 5)
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
                var dueTimeSpan = (quick == 1) ? TimeSpan.FromSeconds(45) : TimeSpan.FromMinutes(_intervalMins);
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
