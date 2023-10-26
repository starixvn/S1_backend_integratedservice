using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerPegasus : BackgroundService
    {
        private readonly ILogger<WagerPegasus> _logger;
        private readonly WagerPegasusBLL _wagerPegasusBll;
        private readonly ThirdPartyBLL _thirdPartyBll;
        private readonly DateTimeHelper _dateTimeHelper;
        private Timer _timer;
        private readonly int intervalMins = 3;
        private readonly int iRangMins = 5;
        private readonly int tpId = (int)Tp.Pegasus;
        private readonly string _tpName = Tp.Pegasus.ToString();
        private readonly int tpType = (int)WagerTimerType.Wager;
        private int _quick;
        private readonly int timeZone = 0;

        public WagerPegasus(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerPegasus>>();
            _wagerPegasusBll = provider.GetRequiredService<WagerPegasusBLL>();
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
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
                _quick = 0;
                var dataTpTimer = _thirdPartyBll.GetThirdPartyTimer(tpId, tpType);
                DateTime timerTime;
                int last;
                if (dataTpTimer == null)
                {
                    last = iRangMins - 1;
                    timerTime = DateTime.Now.AddMinutes(-1 * last);
                }
                else
                {
                    timerTime = dataTpTimer.StartTime;
                    last = iRangMins - 1;
                }
                if (timerTime < DateTime.Now)
                {
                    var time = _dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyy-MM-dd HH:mm:ss", timerTime);
                    var result = await _wagerPegasusBll.GetWagersAsync(time.startTime, time.endTime);
                    if (result)
                    {
                        _thirdPartyBll.SetThirdPartyTimer(tpId, tpType, time.endUnix);
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
                if (_quick == 1)
                {
                    var dueTimeSpan = TimeSpan.FromSeconds(60);
                    _timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    _timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
