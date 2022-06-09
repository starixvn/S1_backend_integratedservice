using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.BLL.KM;
using ThirdPartyWager.Service.BLL.SBO;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerSBO : BackgroundService
    {
        private readonly ILogger<WagerSBO> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ISBOBLL _sboBll;

        private const int TpId = (int)Tp.SBO;
        private const int TimeZone = -4;
        private Timer _sportsBookWagerTimer;
        private Timer _virtualSportsWagerTimer;

        public WagerSBO(ISBOBLL sboBll, ThirdPartyBLL thirdPartyBll, ILogger<WagerSBO> logger, DateTimeHelper dateTimeHelper)
        {
            _sboBll = sboBll;
            _thirdPartyBLL = thirdPartyBll;
            _logger = logger;
            _dateTimeHelper = dateTimeHelper;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _sportsBookWagerTimer = new Timer(DoSportsBookSettle, null, TimeSpan.FromSeconds(1), TimeSpan.Zero);
            _virtualSportsWagerTimer = new Timer(DoVirtualSportsSettle, null, TimeSpan.FromMinutes(1), TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoSportsBookSettle(object? state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", Tp.SBO.ToString());
            var doQuickly = false;
            const int tpType = (int)GameType.SBO_Sports;
            const int dataRangeMins = 15;
            const int intervalMins = 1;
            const int intervalSec = 5;
            try
            {
                var lastTime = _thirdPartyBLL.GetThirdPartyTimer(TpId, tpType);
                var thisTime = lastTime?.StartTime ?? DateTime.Now.AddMinutes(-1 * dataRangeMins);

                if (thisTime < DateTime.Now)
                {
                    thisTime = thisTime.AddMinutes(1);
                    var timeCast = _dateTimeHelper.GetTimeByMinuteRevert(dataRangeMins, TimeZone, "s", thisTime);
                    var isSuccess = await _sboBll.GetWagersAsync(timeCast.startTime, timeCast.endTime, SBOPortfolio.SportsBook);
                    if (isSuccess)
                    {
                        _logger.LogInformation($"SBO {GameType.SBO_Sports} Get Wagers: {timeCast.startTime} ~ {timeCast.endTime}");
                        _thirdPartyBLL.SetThirdPartyTimer(TpId, tpType, timeCast.endUnix);
                    }
                }
                if ((int)DateTime.Now.Subtract(thisTime).TotalMinutes > 5)
                {
                    doQuickly = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WagerSBO.DoSportsBookSettle.Exception");
            }
            finally
            {
                var nextTimeSpan = doQuickly ? TimeSpan.FromSeconds(intervalSec) : TimeSpan.FromMinutes(intervalMins);
                _sportsBookWagerTimer.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }
        private async void DoVirtualSportsSettle(object? state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", Tp.SBO.ToString());
            var doQuickly = false;
            const int tpType = (int)GameType.SBO_VirtualSports;
            const int dataRangeMins = 15;
            const int intervalMins = 1;
            const int intervalSec = 5;
            try
            {
                var lastTime = _thirdPartyBLL.GetThirdPartyTimer(TpId, tpType);
                var thisTime = lastTime?.StartTime ?? DateTime.Now.AddMinutes(-1 * dataRangeMins);

                if (thisTime < DateTime.Now)
                {
                    thisTime = thisTime.AddMinutes(1);
                    var timeCast = _dateTimeHelper.GetTimeByMinuteRevert(dataRangeMins, TimeZone, "s", thisTime);
                    var isSuccess = await _sboBll.GetWagersAsync(timeCast.startTime, timeCast.endTime, SBOPortfolio.VirtualSports);
                    if (isSuccess)
                    {
                        _logger.LogInformation($"SBO {GameType.SBO_VirtualSports} Get Wagers: {timeCast.startTime} ~ {timeCast.endTime}");
                        _thirdPartyBLL.SetThirdPartyTimer(TpId, tpType, timeCast.endUnix);
                    }
                }
                if ((int)DateTime.Now.Subtract(thisTime).TotalMinutes > 5)
                {
                    doQuickly = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WagerSBO.DoVirtualSportsSettle.Exception");
            }
            finally
            {
                var nextTimeSpan = doQuickly ? TimeSpan.FromSeconds(intervalSec) : TimeSpan.FromMinutes(intervalMins);
                _virtualSportsWagerTimer.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }
    }
}