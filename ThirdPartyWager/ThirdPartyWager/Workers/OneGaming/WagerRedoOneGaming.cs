using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;
using Microsoft.Extensions.DependencyInjection;

namespace ThirdPartyWager.Workers
{
    public class WagerRedoOneGaming : BackgroundService
    {
        private readonly ILogger<WagerRedoOneGaming> _logger;
        private readonly WagerOneGamingBLL _wagerOneGamingBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly CommonBLL _commonBLL;
        private readonly IHostEnvironment _environment;
        private Timer timer;
        private readonly int intervalDays = 1;
        private readonly int iRangMins = 1440;
        private readonly int tpId = (int)Tp.OneGaming;
        private readonly string _tpName = Tp.OneGaming.ToString();
        private readonly int tpType = 2;
        private readonly int iDays = 2;
        private readonly int timeZone = 8;

        public WagerRedoOneGaming(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerRedoOneGaming>>();
            _wagerOneGamingBLL = provider.GetRequiredService<WagerOneGamingBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _environment = provider.GetRequiredService<IHostEnvironment>();
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
                    if (!_environment.IsProduction() && item.TpCurrencyId != "IDR")
                    {
                        continue;
                    }

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
                    var dueTimeSpan = TimeSpan.FromSeconds(60);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromDays(intervalDays);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        private async Task<int> Main(string currency)
        {
            var quick = 0;
            var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);

            DateTime timerTime;
            if (dataTpTimer == null)
            {
                timerTime = DateTime.Now.AddDays(-7);
            }
            else
            {
                timerTime = dataTpTimer.StartTime.AddSeconds(1);
            }

            var time = _dateTimeHelper.GetTimeByDay(iRangMins, timeZone, "yyyyMMddHHmmss", timerTime);
            var cvTime = DateTimeOffset.Now.AddDays(iDays * -1).ToOffset(new TimeSpan(timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                var result = await _wagerOneGamingBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                }
            }

            var diffDay = (int)(DateTime.Now.Date - timerTime.Date).TotalDays;
            if (diffDay >= (iDays + 1))
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
