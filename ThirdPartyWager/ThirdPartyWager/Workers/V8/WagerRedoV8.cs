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
    public class WagerRedoV8 : BackgroundService
    {
        private readonly ILogger<WagerRedoV8> logger;
        //private readonly WagerRedoV8BLL wagerRedoV8BLL;
        private readonly WagerV8BLL wagerV8BLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly DateTimeHelper dateTimeHelper;
        private readonly IHostEnvironment environment;
        private readonly Log2File log2File;
        private Timer timer;
        private int intervalMins = 60;
        private int tpId = (int)Tp.V8;
        private int tpType = 2;
        private int quick = 0;
        private int beyond = 120;
        private string logName = Tp.V8.ToString();

        public WagerRedoV8(
            ILogger<WagerRedoV8> logger,
            //WagerRedoV8BLL wagerRedoV8BLL,
            WagerV8BLL wagerV8BLL,
            ThirdPartyBLL thirdPartyBLL,
            DateTimeHelper dateTimeHelper,
            IHostEnvironment environment,
            Log2File log2File
        )
        {
            this.logger = logger;
            //this.wagerRedoV8BLL = wagerRedoV8BLL;
            this.wagerV8BLL = wagerV8BLL;
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
            try
            {
                quick = 0;
                var dataTpTimer = thirdPartyBLL.GetThirdPartyTimer(tpId, tpType);
                DateTime timerTime;
                int last;
                int first = 0;
                if (dataTpTimer == null)
                {
                    first = 1;
                    last = intervalMins;
                    timerTime = DateTime.Now.AddMinutes(-1 * last).AddDays(-3);
                }
                else
                {
                    timerTime = dataTpTimer.StartTime.AddMinutes(1);
                    last = intervalMins;
                }

                if (timerTime < DateTime.Now)
                {
                    var time = dateTimeHelper.GetTimeByHour(last, 0, "yyyy-MM-dd HH:mm:ss", timerTime);
                    //bool result = await wagerRedoV8BLL.RedoWagersAsync(tpId, time.startUnix, time.endUnix);
                    bool result = await wagerV8BLL.GetWagersAsync(tpId, time.startUnix, time.endUnix);
                    if (result)
                    {
                        thirdPartyBLL.SetThirdPartyTimer(tpId, tpType, time.endUnix);
                    }
                    else
                    {
                        if (first == 1)
                        {
                            quick = first;
                        }
                    }
                }
                TimeSpan ts = DateTime.Now - timerTime;
                int diffMin = (int)ts.TotalMinutes;
                if (diffMin >= beyond)
                {
                    quick = 1;
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                log2File.Error(ex.ToString(), logName);
            }
            finally
            {
                if (quick == 1)
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(61);
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
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}