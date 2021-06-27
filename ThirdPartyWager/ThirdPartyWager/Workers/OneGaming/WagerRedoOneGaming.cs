using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly Log2File log2File;
        private Timer timer;
        private int intervalDays = 1;
        private int iRangMins = 1440;
        private int tpId = (int)Tp.OneGaming;
        private int tpType = 2;
        private int iDays = 2;
        private string logName = Tp.OneGaming.ToString();

        public WagerRedoOneGaming(
            ILogger<WagerRedoOneGaming> logger,
            WagerOneGamingBLL wagerOneGamingBLL,
            ThirdPartyBLL thirdPartyBLL,
            DateTimeHelper dateTimeHelper,
            CommonBLL commonBLL,
            IHostEnvironment environment,
            Log2File log2File
        )
        {
            this.logger = logger;
            this.wagerOneGamingBLL = wagerOneGamingBLL;
            this.thirdPartyBLL = thirdPartyBLL;
            this.dateTimeHelper = dateTimeHelper;
            this.commonBLL = commonBLL;
            this.environment = environment;
            this.log2File = log2File;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTask(object state)
        {

            int quick = 0;
            int failed = 0;
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
                            logger.LogError(ex, logName);
                            Interlocked.Increment(ref failed);
                        }
                    }));

                }
                Task t = Task.WhenAll(tasks);
                t.Wait();
                //foreach (var ti in tasks)
                //{
                //    Console.WriteLine("Task {0}: {1}", ti.Id, ti.Status);
                //}
            }
            catch (Exception ex)
            {
                logger.LogError(ex, logName);
                log2File.Error(ex.ToString(), logName);
            }
            finally
            {
                if (quick == 1)
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(60);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromDays(intervalDays);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        private async Task<int> Main(string currency)
        {
            int quick = 0;
            var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);

            DateTime timerTime;
            int last;
            if (dataTpTimer == null)
            {
                timerTime = DateTime.Now.AddDays(-7);
            }
            else
            {
                timerTime = dataTpTimer.StartTime.AddSeconds(1);
            }

            if (timerTime < DateTime.Now.AddDays(iDays * -1))
            {
                var time = dateTimeHelper.GetTimeByDay(iRangMins, 8, "yyyyMMddHHmmss", timerTime);
                bool result = await wagerOneGamingBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency);
                if (result)
                {
                    thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                }
            }

            int diffDay = (int)(DateTime.Now.Date - timerTime.Date).TotalDays;
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
