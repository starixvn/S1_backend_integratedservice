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
using ThirdPartyWager.Extensions;

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
        private Timer _reportTimer;

        private readonly int tpId = (int)Tp.NineW;
        private readonly string _tpName = Tp.NineW.ToString();
        private readonly int tpType = 51;
        private readonly int TimeZone = 8;

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
            _reportTimer = new Timer(GetReportByDate, null, TimeSpan.FromSeconds(7), TimeSpan.Zero);

            return Task.CompletedTask;
        }

        private async void DoSettle(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var timerType = 1;
            var dataRangeMins = 15;
            var intervalMins = 3;
            var nextTimeSpan = TimeSpan.FromDays(intervalMins);
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
                    nextTimeSpan = TimeSpan.FromSeconds(10);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WagerNineW.DoSettle.Exception");
                TimeSpan.FromMinutes(1);
            }
            finally
            {
                _settleTimer.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }

        private async void DoSettleByDate(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var timerType = 2;
            var dataRangeMins = 1441;
            var intervalDays = 1;
            var nextTimeSpan = TimeSpan.FromDays(intervalDays);

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
                    nextTimeSpan = TimeSpan.FromMinutes(1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WagerNineW.DoSettleByDate.Exception");
                nextTimeSpan = TimeSpan.FromHours(1);
            }
            finally
            {
                _settleTimerByDate.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }

        private async void DoReSettle(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var timerType = 3;
            var dataRangeMins = 180;
            var intervalHours = 1;
            var nextTimeSpan = TimeSpan.FromDays(intervalHours);
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
                    nextTimeSpan = TimeSpan.FromMinutes(1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WagerNineW.DoReSettle.Exception");
                nextTimeSpan = TimeSpan.FromMinutes(5);
            }
            finally
            {
                _reSttleTimer.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }

        private async void GetReportByDate(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);         
            var timerType = 4;
            var intervalDays = 1;
            var nextTimeSpan = TimeSpan.FromDays(intervalDays);
            try
            {
                var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByGameType(tpId, timerType, tpType);
                var date = lastTime?.StartTime ?? DateTime.UtcNow.BeginningOfTheDay();

                if (date < DateTime.UtcNow)
                {         
                    var isSuccess = await _nineWService.GetReportAsync(date);
                    if (isSuccess)
                    {
                        _logger.LogInformation($"9W Get {date:yyyy-MM-dd} Report");
                        _thirdPartyBLL.SetThirdPartyTimerByGameType(tpId, timerType, new DateTimeOffset(date.AddDays(intervalDays)).ToUnixTimeSeconds(), tpType);
                    }
                }

                if ((int)DateTime.UtcNow.Subtract(date).TotalDays > intervalDays)
                {
                    nextTimeSpan = TimeSpan.FromSeconds(1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WagerNineW.GetReportByDate.Exception");
                nextTimeSpan = TimeSpan.FromHours(1);
            }
            finally
            {     
                _reportTimer.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _settleTimer?.Change(Timeout.Infinite, 0);
            _settleTimerByDate?.Change(Timeout.Infinite, 0);
            _reSttleTimer?.Change(Timeout.Infinite, 0);
            _reportTimer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}