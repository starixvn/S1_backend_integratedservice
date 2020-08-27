using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerCF : BackgroundService
    {
        private readonly ILogger<WagerCF> logger;
        private readonly WagerCFBLL wagerCFBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly DateTimeHelper dateTimeHelper;
        private readonly IHostEnvironment environment;
        private Timer timer;
        private int intervalMins = 3;

        public WagerCF(
            ILogger<WagerCF> logger,
            WagerCFBLL wagerCFBLL,
            ThirdPartyBLL thirdPartyBLL,
            DateTimeHelper dateTimeHelper,
            IHostEnvironment environment
        )
        {
            this.logger = logger;
            this.wagerCFBLL = wagerCFBLL;
            this.thirdPartyBLL = thirdPartyBLL;
            this.dateTimeHelper = dateTimeHelper;
            this.environment = environment;
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
                int tpId = (int)Tp.CF;
                var dataTpTimer = thirdPartyBLL.GetThirdPartyTimer(tpId, 1);

                DateTime timerTime;
                int last;

                if (dataTpTimer == null)
                {
                    timerTime = DateTime.Now.AddMinutes(-1 * intervalMins);
                    last = intervalMins;
                }
                else
                {
                    timerTime = dataTpTimer.StartTime;
                    last = intervalMins;
                }

                if (timerTime < DateTime.Now)
                {
                    var time = dateTimeHelper.GetTimeByMinute(last, 8, "yyyy-MM-dd HH:mm", timerTime);
                    bool result = await wagerCFBLL.GetWagersAsync(tpId, time.startTime, time.endTime);

                    if (result == true)
                    {
                        thirdPartyBLL.SetThirdPartyTimer(tpId, 1, time.endUnix);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                timer.Change(dueTimeSpan, TimeSpan.Zero);
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
