using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerNineW : BackgroundService
    {
        private readonly ILogger<WagerNineW> _logger;
        private readonly NineWBLL _nineWService;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private Timer _settleTimer;
        private Timer _settleTimerByDate;
        private Timer _reSttleTimer;

        private readonly int tpId = (int)Tp.NineW;
        private readonly int tpType = 51;
        private readonly int TimeZone = 8;
        private readonly string _tpName = Tp.NineW.ToString();

        public WagerNineW(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerNineW>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _nineWService = provider.GetRequiredService<NineWBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _settleTimer = new Timer(DoSettle, null, TimeSpan.FromSeconds(1), TimeSpan.Zero);
            _settleTimerByDate = new Timer(DoSettleByDate, null, TimeSpan.FromSeconds(3), TimeSpan.Zero);
            _reSttleTimer = new Timer(DoReSettle, null, TimeSpan.FromSeconds(5), TimeSpan.Zero);

            return Task.CompletedTask;
        }

        private async void DoSettle(object state)
        {
            LogContext.PushProperty("Tp", _tpName);
            var doQuickly = false;
            var timerType = 1;
            var dataRangeMins = 15;
            var intervalMins = 3;
            try
            {
                var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByGameType(tpId, timerType, tpType);
                var thisTime = lastTime?.StartTime.AddMinutes((dataRangeMins - intervalMins) * -1) ?? DateTime.Now.AddDays(-1);
                if (thisTime < DateTime.Now)
                {
                    var timeCast = _dateTimeHelper.GetTimeByMinute(dataRangeMins, TimeZone, "yyyy-MM-dd HH:mm", thisTime);
                    var isSuccess = await _nineWService.GetWagersAsync(timeCast.startTime, timeCast.endTime);
                    if (isSuccess)
                    {
                        _logger.LogInformation($"9W Get Wagers: {timeCast.startTime} ~ {timeCast.endTime}");
                        _thirdPartyBLL.SetThirdPartyTimerByGameType(tpId, timerType, timeCast.endUnix, tpType);
                    }
                }

                if ((int)DateTime.Now.Subtract(thisTime).TotalMinutes > dataRangeMins)
                {
                    doQuickly = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WagerNineW.DoSettle.Exception");
            }
            finally
            {
                var nextTimeSpan = doQuickly ? TimeSpan.FromSeconds(30) : TimeSpan.FromMinutes(intervalMins);
                _settleTimer.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }

        private async void DoSettleByDate(object state)
        {
            LogContext.PushProperty("Tp", _tpName);
            var doQuickly = false;
            var timerType = 2;
            var dataRangeMins = 1441;
            var intervalDays = 1;
            try
            {
                var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByGameType(tpId, timerType, tpType);
                var thisTime = lastTime?.StartTime ?? DateTime.Now.Date.AddDays(-3);

                if (thisTime < DateTime.Now)
                {
                    var timeCast = _dateTimeHelper.GetTimeByDay(dataRangeMins, TimeZone, "yyyy-MM-dd HH:mm", thisTime);
                    var isSuccess = await _nineWService.GetWagersAsync(timeCast.startTime, timeCast.endTime);
                    if (isSuccess)
                    {
                        _logger.LogInformation($"9W Get Wagers byDate: {timeCast.startTime} ~ {timeCast.endTime}");
                        _thirdPartyBLL.SetThirdPartyTimerByGameType(tpId, timerType, timeCast.endUnix, tpType);
                    }
                }

                if ((int)DateTime.Now.Subtract(thisTime).TotalMinutes > dataRangeMins)
                {
                    doQuickly = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WagerNineW.DoSettleByDate.Exception");
            }
            finally
            {
                var nextTimeSpan = doQuickly ? TimeSpan.FromMinutes(1) : TimeSpan.FromDays(intervalDays);
                _settleTimerByDate.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }

        private async void DoReSettle(object state)
        {
            LogContext.PushProperty("Tp", _tpName);
            var doQuickly = false;
            var timerType = 3;
            var dataRangeMins = 180;
            var intervalHours = 1;
            try
            {
                var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByGameType(tpId, timerType, tpType);
                var thisTime = lastTime?.StartTime ?? DateTime.Now.AddDays(-3);

                var timeCast = _dateTimeHelper.GetTimeByHour(dataRangeMins, TimeZone, "yyyy-MM-dd HH:mm", thisTime);
                var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(TimeZone, 0, 0)).ToUnixTimeSeconds();
                if (timeCast.endUnix < cvTime)
                {
                    var isSuccess = await _nineWService.GetReSettleWagersAsync(timeCast.startTime, timeCast.endTime);
                    if (isSuccess)
                    {
                        _logger.LogInformation($"9W Get ReSettle Wagers : {timeCast.startTime} ~ {timeCast.endTime}");
                        _thirdPartyBLL.SetThirdPartyTimerByGameType(tpId, timerType, timeCast.endUnix, tpType);
                    }
                }

                if ((int)DateTime.Now.Subtract(thisTime).TotalMinutes > dataRangeMins)
                {
                    doQuickly = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WagerNineW.DoReSettle.Exception");
            }
            finally
            {
                var nextTimeSpan = doQuickly ? TimeSpan.FromMinutes(1) : TimeSpan.FromHours(intervalHours);
                _reSttleTimer.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _settleTimer?.Change(Timeout.Infinite, 0);
            _settleTimerByDate?.Change(Timeout.Infinite, 0);
            _reSttleTimer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}