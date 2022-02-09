using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Context;
using ThirdPartyWager.Extensions;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerTCG : BackgroundService
    {
        private readonly ILogger<WagerTCG> _logger;
        private readonly TCGBLL _tcgBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;

        private Timer _settleTimerTHB;
        private Timer _settleTimerMYR;
        private Timer _settleTimerCNY;
        private Timer _settleTimerVNDK;
        private Timer _settleTimerIDRK;
        private Timer _settleTimerINR;

        private readonly int tpId = (int)Tp.TCG;
        private readonly string _tpName = Tp.TCG.ToString();
        private readonly int TimeZone = 8;

        public WagerTCG(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerTCG>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tcgBLL = provider.GetRequiredService<TCGBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _settleTimerMYR = new Timer(DoSettle, "MYR", TimeSpan.FromSeconds(1), TimeSpan.Zero);
            _settleTimerTHB = new Timer(DoSettle, "THB", TimeSpan.FromSeconds(3), TimeSpan.Zero);
            _settleTimerCNY = new Timer(DoSettle, "CNY", TimeSpan.FromSeconds(5), TimeSpan.Zero);
            _settleTimerVNDK = new Timer(DoSettle, "VNDK", TimeSpan.FromSeconds(7), TimeSpan.Zero);
            _settleTimerIDRK = new Timer(DoSettle, "IDRK", TimeSpan.FromSeconds(9), TimeSpan.Zero);
            _settleTimerINR = new Timer(DoSettle, "INR", TimeSpan.FromSeconds(11), TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoSettle(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var currency = (string)state;
            var doQuickly = false;
            var timerType = 1;
            var dataRangeMins = 5;
            var intervalMins = 2;
            try
            {
                var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, timerType, currency);
                var thisTime = lastTime?.StartTime.AddMinutes(dataRangeMins) ?? DateTime.Now.BeginningOfTheDay();

                if (thisTime < DateTime.Now.AddMinutes(-10))
                {
                    var batchTime = new DateTimeOffset(thisTime);
                    var timezoneBatchTime = batchTime.ToOffset(new TimeSpan(TimeZone, 0, 0));
                    var isSuccess = currency == "VNDK" ? 
                        await _tcgBLL.GetWagersAsync<TCGVNWagerData>(timezoneBatchTime, currency):
                        await _tcgBLL.GetWagersAsync<TCGSEAWagerData>(timezoneBatchTime, currency);
                    if (isSuccess)
                    {
                        _logger.LogInformation($"TCG {currency} Get {timezoneBatchTime:yyyyMMddHHmm} batch file success.");
                        _thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, timerType, batchTime.ToUnixTimeSeconds(), currency);
                    }
                }

                if ((int)DateTime.Now.Subtract(thisTime).TotalMinutes > dataRangeMins)
                {
                    doQuickly = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"WagerTCG.DoSettle.{currency}.Exception");
            }
            finally
            {
                var nextTimeSpan = doQuickly ? TimeSpan.FromSeconds(5) : TimeSpan.FromMinutes(intervalMins);
                switch (currency)
                {
                    case "MYR":
                        _settleTimerMYR.Change(nextTimeSpan, TimeSpan.Zero);
                        break;
                    case "THB":
                        _settleTimerTHB.Change(nextTimeSpan, TimeSpan.Zero);
                        break;
                    case "CNY":
                        _settleTimerCNY.Change(nextTimeSpan, TimeSpan.Zero);
                        break;
                    case "VNDK":
                        _settleTimerVNDK.Change(nextTimeSpan, TimeSpan.Zero);
                        break;
                    case "IDRK":
                        _settleTimerIDRK.Change(nextTimeSpan, TimeSpan.Zero);
                        break;
                    case "INR":
                        _settleTimerINR.Change(nextTimeSpan, TimeSpan.Zero);
                        break;
                    default:
                        break;
                }
               
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _settleTimerMYR?.Change(Timeout.Infinite, 0);
            _settleTimerTHB?.Change(Timeout.Infinite, 0);
            _settleTimerCNY?.Change(Timeout.Infinite, 0);
            _settleTimerVNDK?.Change(Timeout.Infinite, 0);
            _settleTimerIDRK?.Change(Timeout.Infinite, 0);
            _settleTimerINR?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}