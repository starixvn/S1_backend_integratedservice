using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyWager.Extensions;

namespace ThirdPartyWager.Workers
{
    public class WagerUnsettleCheckAllBet : BackgroundService
    {
        private readonly ILogger<WagerUnsettleCheckAllBet> _logger;
        private readonly WagerAllBetBLL _wagerAllBetBLL;
        private Timer _timer;
        private readonly string _tpName = Tp.Allbet.ToString();
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly int tpId = (int)Tp.Allbet;
        private readonly int timerType = (int)WagerTimerType.CheckSettle;

        public WagerUnsettleCheckAllBet(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerUnsettleCheckAllBet>>();
            _wagerAllBetBLL = provider.GetRequiredService<WagerAllBetBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoUnsettleCheck, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoUnsettleCheck(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var intervalHours = 1;
            var nextTimeSpan = TimeSpan.FromHours(intervalHours);

            try
            {
                var lastTime = _thirdPartyBLL.GetThirdPartyTimer(tpId, timerType);
                var thisTime = lastTime?.StartTime ?? DateTime.UtcNow.AddHours(-3);
                if (thisTime < DateTime.UtcNow)
                {
                    var (isSuccess, doQuick) = await _wagerAllBetBLL.UnsettleCheck();
                    if (isSuccess)
                    {
                        _thirdPartyBLL.SetThirdPartyTimer(tpId, timerType, new DateTimeOffset(thisTime.AddHours(intervalHours)).ToUnixTimeSeconds());
                    }

                    if (doQuick)
                    {
                        nextTimeSpan = TimeSpan.FromMinutes(1);
                    }
                }
              
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Allbet.SettleCheck.Exception");
                nextTimeSpan = TimeSpan.FromMinutes(1);
            }
            finally
            {
                _timer.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
