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
    public class WagerRedoAG : BackgroundService
    {
        private readonly ILogger<WagerRedoAG> logger;
        private readonly WagerRedoAGBLL wagerRedoAGBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly DateTimeHelper dateTimeHelper;
        private readonly IHostEnvironment environment;
        private readonly Log2File log2File;
        private Timer timer;
        private int intervalMins = 60;
        private int tpId = (int)Tp.AG;
        private int tpType = 2;
        private int quick = 0;
        private readonly int timeZone = -4;
        private string logName = Tp.AG.ToString();

        public WagerRedoAG(
            ILogger<WagerRedoAG> logger,
            WagerRedoAGBLL wagerRedoAGBLL,
            ThirdPartyBLL thirdPartyBLL,
            DateTimeHelper dateTimeHelper,
            IHostEnvironment environment,
            Log2File log2File
        )
        {
            this.logger = logger;
            this.wagerRedoAGBLL = wagerRedoAGBLL;
            this.thirdPartyBLL = thirdPartyBLL;
            this.dateTimeHelper = dateTimeHelper;
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
            //if (!environment.IsProduction())
            //{
            //    return;
            //}

            try
            {
                quick = 0;
                var dataTpTimer = thirdPartyBLL.GetThirdPartyTimer(tpId, tpType);
                DateTime timerTime;
                int last;
                if (dataTpTimer == null)
                {
                    timerTime = DateTime.Now.AddDays(-1);
                    last = intervalMins;
                }
                else
                {
                    timerTime = dataTpTimer.StartTime.AddSeconds(1);
                    last = intervalMins;
                }

                var time = dateTimeHelper.GetTimeByHour(last, timeZone, "yyyy-MM-dd HH:mm:ss", timerTime);
                var cvTime = DateTime.UtcNow.AddHours(timeZone);
                if (Convert.ToDateTime(time.endTime) < cvTime)
                {
                    bool result = await wagerRedoAGBLL.RedoWagersAsync(tpId, time.startTime, time.endTime, time.startUnix, time.endUnix);
                    if (result)
                    {
                        thirdPartyBLL.SetThirdPartyTimer(tpId, tpType, time.endUnix);
                    }
                }
                TimeSpan ts = DateTime.Now - timerTime;
                int diffMin = (int)ts.TotalMinutes;
                if (diffMin >= 120)
                {
                    quick = 1;
                }
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
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(120);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            //WriteToFile("WriteToFileHostedService: Process Stopped");
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
