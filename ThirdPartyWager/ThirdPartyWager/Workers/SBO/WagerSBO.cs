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
        private const int TpType = 77;
        private const int TimeZone = 0;
        private Timer _wagerTimer;

        public WagerSBO(ISBOBLL sboBll, DateTimeHelper dateTimeHelper, ThirdPartyBLL thirdPartyBll, ILogger<WagerSBO> logger)
        {
            _sboBll = sboBll;
            _dateTimeHelper = dateTimeHelper;
            _thirdPartyBLL = thirdPartyBll;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _wagerTimer = new Timer(DoSettle, null, TimeSpan.FromSeconds(1), TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoSettle(object? state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", Tp.SBO.ToString());
            var doQuickly = false;
            var timerType = 1; //Settle
            var dataRangeMins = 20;
            var intervalMins = 3;
            var intervalSec = 30;
            try
            {
                var lastTime = _thirdPartyBLL.GetThirdPartyTimer(TpId, TpType);
                var thisTime = lastTime?.StartTime ?? DateTime.Now.AddMinutes(-1 * dataRangeMins);

                if (thisTime < DateTime.Now)
                {
                    var startTime = thisTime.AddMinutes(-20);
                    var isSuccess = await _sboBll.GetWagersAsync(thisTime.AddMinutes(-20), thisTime);
                    if (isSuccess)
                    {
                        var endTimestamp = new DateTimeOffset(thisTime).ToUnixTimeSeconds();
                        _logger.LogInformation($"SBO Get Wagers: {startTime} ~ {thisTime}");
                        _thirdPartyBLL.SetThirdPartyTimer(TpId, TpType, endTimestamp);
                    }
                }
                if ((int)DateTime.Now.Subtract(thisTime).TotalMinutes > dataRangeMins)
                {
                    doQuickly = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WagerSBO.DoSettle.Exception");
            }
            finally
            {
                var nextTimeSpan = doQuickly ? TimeSpan.FromSeconds(intervalSec) : TimeSpan.FromMinutes(intervalMins);
                _wagerTimer.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }
    }
}