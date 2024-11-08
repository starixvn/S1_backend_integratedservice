using Serilog.Context;

using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerNextspin : BackgroundService
    {
        private readonly ILogger<WagerNextspin> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly INextspinBLL _nextspinBLL;

        private const int TpId = (int)Tp.Nextspin;
        private const int TimeZone = 8;
        private Timer _rngWagerTimer;

        public WagerNextspin(ThirdPartyBLL thirdPartyBll, DateTimeHelper dateTimeHelper, INextspinBLL nextspinBLL, ILogger<WagerNextspin> logger)
        {
            _thirdPartyBLL = thirdPartyBll;
            _dateTimeHelper = dateTimeHelper;
            _nextspinBLL = nextspinBLL;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _rngWagerTimer = new Timer(QueryWager, GameType.Nextspin_RNG, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void QueryWager(object? state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", Tp.Nextspin.ToString());
            var doQuickly = false;
            var tpType = WagerTimerType.Wager;
            const int dataRangeMins = 15; //查詢區間15分鐘內的資料
            const int intervalMins = 1;   //每次查詢間隔1分鐘
            const int intervalSec = 5;    //doQuickly 每次查詢間隔5秒
            try
            {
                var lastTime = _thirdPartyBLL.GetThirdPartyTimer(TpId, (int)tpType);
                var thisTime = lastTime?.StartTime ?? DateTime.UtcNow;

                if ((int)DateTime.Now.Subtract(thisTime).TotalMinutes > 3)
                {
                    doQuickly = true;
                }
                if (thisTime < DateTime.UtcNow)
                {
                    thisTime = thisTime.AddMinutes(1);
                    var timeCast = _dateTimeHelper.GetTimeByMinuteRevert(dataRangeMins, TimeZone, "yyyyMMddTHHmmss", thisTime);
                    var isSuccess = await _nextspinBLL.GetWagersAsync(timeCast.startTime, timeCast.endTime);
                    if (isSuccess)
                    {
                        _logger.LogInformation($"{Tp.Nextspin} {tpType} Get Wagers: {timeCast.startTime} ~ {timeCast.endTime}");
                        _thirdPartyBLL.SetThirdPartyTimer(TpId, (int)tpType, timeCast.endUnix);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{tpType} WagerNextspin.QueryWager.Exception");
            }
            finally
            {
                var nextTimeSpan = doQuickly ? TimeSpan.FromSeconds(intervalSec) : TimeSpan.FromMinutes(intervalMins);
                _rngWagerTimer.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }
    }
}