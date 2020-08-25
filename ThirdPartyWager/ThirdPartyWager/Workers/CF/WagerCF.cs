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
                Console.WriteLine(dataTpTimer);
                DateTime timerTime;
                int first;
                int last;
                if (dataTpTimer == null)
                {
                    timerTime = DateTime.Now;
                    first = -10;
                    last = -5;
                }
                else
                {
                    timerTime = dataTpTimer.StartTime;
                    first = 0;
                    last = 5;
                }
                var time = dateTimeHelper.GetTimeByMinute(first, last, 8, "yyyy-MM-dd HH:mm", timerTime);
                var startTime = $"{time["startTime"]}";
                var endTime = $"{time["endTime"]}";
                bool result = await wagerCFBLL.GetWagersAsync(tpId, startTime, endTime);
                if (result == true)
                {
                    var setTime = time["endUnix"];
                    thirdPartyBLL.SetThirdPartyTimer(tpId, 1, setTime);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(5);

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
