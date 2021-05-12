using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerNineW : BackgroundService
    {
        private readonly ILogger<WagerNineW> _logger;
        private readonly NineWService _nineWService;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private Timer _settleTimer;
        private Timer _reSttleTimer;

        private readonly int intervalMins = 5;
        private const int DataRangeMins = 60;
        private readonly int tpId = (int)Tp.NineW;
        private readonly int tpType = 51;
        private readonly int beyond = 6;
        private readonly int TimeZone = 8;

        public WagerNineW(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerNineW>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _nineWService = provider.GetRequiredService<NineWService>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _settleTimer = new Timer(DoSettle, null, TimeSpan.Zero, TimeSpan.Zero);
            _reSttleTimer = new Timer(DoReSettle, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoSettle(object state)
        {
            var doQuickly = false;
            var timerType = 1;
            try
            {
                var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByGameType(tpId, timerType, tpType);              
                var thisTime = lastTime?.StartTime ?? DateTime.Now.AddDays(-1);

                if (thisTime < DateTime.Now)
                {
                    var timeCast = _dateTimeHelper.GetTimeByMinute(DataRangeMins, TimeZone, "yyyy-MM-dd HH:mm", thisTime);    
                    var isSuccess = await _nineWService.GetWagersAsync(timeCast.startTime, timeCast.endTime);
                    if (isSuccess)
                    {
                        _logger.LogInformation($"9W Get Wagers: {timeCast.startTime} ~ {timeCast.endTime}");
                        _thirdPartyBLL.SetThirdPartyTimerByGameType(tpId, timerType, timeCast.endUnix, tpType);
                    }
                }

                if ((int)DateTime.Now.Subtract(thisTime).TotalMinutes > beyond)
                {
                    doQuickly = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WagerNineW.Exception");
            }
            finally
            {
                var nextTimeSpan = doQuickly ? TimeSpan.FromSeconds(30) : TimeSpan.FromMinutes(intervalMins);
                _settleTimer.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }

        private async void DoReSettle(object state)
        { 
        
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _settleTimer?.Change(Timeout.Infinite, 0);
            _reSttleTimer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}