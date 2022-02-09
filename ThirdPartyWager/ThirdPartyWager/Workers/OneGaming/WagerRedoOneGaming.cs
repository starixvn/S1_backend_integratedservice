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

namespace ThirdPartyWager.Workers
{
    public class WagerRedoOneGaming : BackgroundService
    {
        private readonly ILogger<WagerRedoOneGaming> logger;
        private readonly WagerOneGamingBLL wagerOneGamingBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly DateTimeHelper dateTimeHelper;
        private readonly CommonBLL commonBLL;
        private readonly IHostEnvironment environment;
        private Timer timer;
        private int intervalDays = 1;
        private int iRangMins = 1440;
        private int tpId = (int)Tp.OneGaming;
        private readonly string _tpName = Tp.OneGaming.ToString();
        private int tpType = 2;
        private int iDays = 2;
        private readonly int timeZone = 8;

        public WagerRedoOneGaming(
            ILogger<WagerRedoOneGaming> logger,
            WagerOneGamingBLL wagerOneGamingBLL,
            ThirdPartyBLL thirdPartyBLL,
            DateTimeHelper dateTimeHelper,
            CommonBLL commonBLL,
            IHostEnvironment environment
        )
        {
            this.logger = logger;
            this.wagerOneGamingBLL = wagerOneGamingBLL;
            this.thirdPartyBLL = thirdPartyBLL;
            this.dateTimeHelper = dateTimeHelper;
            this.commonBLL = commonBLL;
            this.environment = environment;
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
                var tpCurrencyList = commonBLL.GetTpCurrencyList(tpId);

                foreach (var item in tpCurrencyList)
                {
                    if (!environment.IsProduction())
                    {
                        if (item.TpCurrencyId != "IDR")
                        {
                            continue;
                        }
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
                            logger.LogError(ex, ex.Message);
                            Interlocked.Increment(ref failed);
                        }
                    }));

                }
                var t = Task.WhenAll(tasks);
                t.Wait();
                //foreach (var ti in tasks)
                //{
                //    Console.WriteLine("Task {0}: {1}", ti.Id, ti.Status);
                //}
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
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
            var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);

            DateTime timerTime;
            if (dataTpTimer == null)
            {
                timerTime = DateTime.Now.AddDays(-7);
            }
            else
            {
                timerTime = dataTpTimer.StartTime.AddSeconds(1);
            }

            var time = dateTimeHelper.GetTimeByDay(iRangMins, timeZone, "yyyyMMddHHmmss", timerTime);
            var cvTime = DateTimeOffset.Now.AddDays(iDays * -1).ToOffset(new TimeSpan(timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                var result = await wagerOneGamingBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency);
                if (result)
                {
                    thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
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
