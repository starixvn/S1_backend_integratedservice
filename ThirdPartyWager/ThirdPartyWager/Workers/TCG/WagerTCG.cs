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
using System.Collections.Generic;

namespace ThirdPartyWager.Workers
{
    public class WagerTCG : BackgroundService
    {
        private readonly ILogger<WagerTCG> _logger;
        private readonly TCGBLL _tcgBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;

        private Timer _timer;

        private readonly int tpId = (int)Tp.TCG;
        private readonly string _tpName = Tp.TCG.ToString();
        private readonly int TimeZone = 8;

        public WagerTCG(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerTCG>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tcgBLL = provider.GetRequiredService<TCGBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
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
            var quick = 0;
            var failed = 0;
            var tasks = new List<Task>();
            var intervalMins = 2;
            try
            {
                var tpCurrencyList = _commonBLL.GetTpCurrencyList(tpId);

                foreach (var item in tpCurrencyList)
                {
                    var currency = item.TpCurrencyId;
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            var res = Main(currency);
                            if (res.Result == 1)
                            {
                                quick = 1;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"WagerTCG.DoSettle.{currency}.Exception");
                            Interlocked.Increment(ref failed);
                        }
                    }));
                }
                var t = Task.WhenAll(tasks);
                t.Wait();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = (quick == 1) ? TimeSpan.FromSeconds(5) : TimeSpan.FromMinutes(intervalMins);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private async Task<int> Main(string currency)
        {
            var quick = 0;
            var timerType = (int)WagerTimerType.Wager;
            var dataRangeMins = 5;

            var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, timerType, currency);
            var thisTime = lastTime?.StartTime.AddMinutes(dataRangeMins) ?? DateTime.Now.BeginningOfTheDay();

            if (thisTime < DateTime.Now.AddMinutes(-10))
            {
                var batchTime = new DateTimeOffset(thisTime);
                var timezoneBatchTime = batchTime.ToOffset(new TimeSpan(TimeZone, 0, 0));
                var isSuccess = currency == "VNDK" ?
                    await _tcgBLL.GetWagersAsync<TCGVNWagerData>(timezoneBatchTime, currency) :
                    await _tcgBLL.GetWagersAsync<TCGSEAWagerData>(timezoneBatchTime, currency);
                if (isSuccess)
                {
                    _logger.LogInformation($"TCG {currency} Get {timezoneBatchTime:yyyyMMddHHmm} batch file success.");
                    _thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, timerType, batchTime.ToUnixTimeSeconds(), currency);
                }
            }

            if ((int)DateTime.Now.Subtract(thisTime).TotalMinutes > dataRangeMins)
            {
                quick = 1;
            }
            return quick;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}