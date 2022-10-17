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
using System.Collections.Generic;

namespace ThirdPartyWager.Workers
{

    public class WagerAT : BackgroundService
    {
        private readonly ILogger<WagerAT> _logger;
        private readonly WagerIconicBLL _wagerIconicBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly CommonBLL _commonBLL;
        private Timer timer;
        private readonly int intervalMins = 2;
        private readonly int iRangMins = 20;
        private readonly int tpId = (int)Tp.Iconic;
        private readonly string _tpName = Tp.Iconic.ToString();
        private readonly int tpType = (int)WagerTimerType.Wager;
        private readonly int beyond = 5;
        private readonly int timeZone = 0;

        public WagerAT(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerAT>>();
            _wagerIconicBLL = provider.GetRequiredService<WagerIconicBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTask(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var quick = 0;
            var failed = 0;
            var tasks = new List<Task>();
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
                            _logger.LogError(ex, ex.Message);
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
                if (quick == 1)
                {
                    var dueTimeSpan = TimeSpan.FromSeconds(45);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        private async Task<int> Main(string currency)
        {
            var quick = 0;
            var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);

            DateTime timerTime;
            int last = iRangMins - 1;
            if (dataTpTimer == null)
            {
                timerTime = DateTime.Now.AddMinutes(-1 * last);
            }
            else
            {
                timerTime = dataTpTimer.StartTime;
            }

            var time = _dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyy-MM-dd HH:mm:ss", timerTime);
            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                var result = await _wagerIconicBLL.GetWagersV2Async(tpId, time.startUnix, time.endUnix, currency);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                }
            }

            var ts = DateTime.Now - timerTime;
            var diffMin = (int)ts.TotalMinutes;
            if (diffMin > beyond)
            {
                quick = 1;
            }

            return quick;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
