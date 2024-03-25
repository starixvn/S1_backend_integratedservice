using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerOne789 : BackgroundService
    {
        private Timer _timerLT;
        private Timer _timerLT_789;
        private Timer _timerLC;
        private readonly ILogger<WagerOne789> _logger;
        private readonly WagerOne789BLL _wagerOne789BLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _tpId = (int)Tp.One789;
        private readonly string _tpName = Tp.One789.ToString();
        private readonly int _tpType = (int)WagerTimerType.Wager;
        private readonly int _intervalMins = 3;
        private readonly int _overlap = 5;
        private readonly int _iRangMins = 20;
        private readonly int _timeZone = 0;
        private readonly string _timeFormat = "yyyy-MM-ddTHH:mm:ssZ";

        public WagerOne789(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerOne789>>();
            _wagerOne789BLL = provider.GetRequiredService<WagerOne789BLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerLT = new Timer(DoTaskLT, null, TimeSpan.Zero, TimeSpan.Zero);
            _timerLT_789 = new Timer(DoTaskLT_789, null, TimeSpan.Zero, TimeSpan.Zero);
            _timerLC = new Timer(DoTaskLC, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTaskLT(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var gameType = (int)GameType.One789_Lottery;
            var quick = 0;

            try
            {
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimerReferenceByGameType(_tpId, _tpType, gameType);
                int last = (dataTpTimer == null) ? _iRangMins - 1 : 45;
                DateTime timerTime = (dataTpTimer == null) ? DateTime.Now.AddMinutes(-1 * last) : dataTpTimer.StartTime.AddMinutes(-1 * _overlap);

                if (timerTime < DateTime.Now)
                {
                    var time = _dateTimeHelper.GetTimeByMinute(last, _timeZone, _timeFormat, timerTime);
                    var result = await _wagerOne789BLL.GetWagersAsync(time.startTime, time.endTime, gameType);
                    if (result)
                    {
                        _thirdPartyBLL.SetThirdPartyTimerByGameType(_tpId, _tpType, time.endUnix, gameType);
                    }
                }
                var ts = DateTime.Now - timerTime;
                var diffMin = (int)ts.TotalMinutes;
                if (diffMin > 8)
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
                var dueTimeSpan = (quick == 1) ? TimeSpan.FromSeconds(45) : TimeSpan.FromMinutes(30);
                _timerLT.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private async void DoTaskLT_789(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var gameType = (int)GameType.One789_Lottery_789;
            var quick = 0;

            try
            {
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimerReferenceByGameType(_tpId, _tpType, gameType);
                int last = _iRangMins - 1;
                DateTime timerTime = (dataTpTimer == null) ? DateTime.Now.AddMinutes(-1 * last) : dataTpTimer.StartTime.AddMinutes(-1 * _overlap);

                if (timerTime < DateTime.Now)
                {
                    var time = _dateTimeHelper.GetTimeByMinute(last, _timeZone, _timeFormat, timerTime);
                    var result = await _wagerOne789BLL.GetWagersAsync(time.startTime, time.endTime, gameType);
                    if (result)
                    {
                        _thirdPartyBLL.SetThirdPartyTimerByGameType(_tpId, _tpType, time.endUnix, gameType);
                    }
                }
                var ts = DateTime.Now - timerTime;
                var diffMin = (int)ts.TotalMinutes;
                if (diffMin > 8)
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
                var dueTimeSpan = (quick == 1) ? TimeSpan.FromSeconds(45) : TimeSpan.FromMinutes(8);
                _timerLT_789.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private async void DoTaskLC(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var gameType = (int)GameType.One789_LiveCasino;
            var quick = 0;

            try
            {
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimerReferenceByGameType(_tpId, _tpType, gameType);
                int last = _iRangMins - 1;
                DateTime timerTime = (dataTpTimer == null) ? DateTime.Now.AddMinutes(-1 * last) : dataTpTimer.StartTime.AddMinutes(-1 * _overlap);

                if (timerTime < DateTime.Now)
                {
                    var time = _dateTimeHelper.GetTimeByMinute(last, _timeZone, _timeFormat, timerTime);
                    var result = await _wagerOne789BLL.GetWagersAsync(time.startTime, time.endTime, gameType);
                    if (result)
                    {
                        _thirdPartyBLL.SetThirdPartyTimerByGameType(_tpId, _tpType, time.endUnix, gameType);
                    }
                }
                var ts = DateTime.Now - timerTime;
                var diffMin = (int)ts.TotalMinutes;
                if (diffMin > 8)
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
                _timerLC.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerLT?.Change(Timeout.Infinite, 0);
            _timerLT_789?.Change(Timeout.Infinite, 0);
            _timerLC?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
