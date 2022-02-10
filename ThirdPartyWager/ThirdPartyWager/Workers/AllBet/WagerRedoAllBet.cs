using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerRedoAllBet : BackgroundService
    {
        private readonly ILogger<WagerRedoAllBet> logger;
        private readonly WagerRedoAllBetBLL wagerRedoAllBetBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly DateTimeHelper dateTimeHelper;
        private Timer timer;
        private int intervalMins = 60;
        private int tpId = (int)Tp.Allbet;
        private readonly string _tpName = Tp.Allbet.ToString();
        private int tpType = 2;
        private int quick = 0;
        private readonly int timeZone = 8;

        public WagerRedoAllBet(
            ILogger<WagerRedoAllBet> logger,
            WagerRedoAllBetBLL wagerRedoAllBetBLL,
            ThirdPartyBLL thirdPartyBLL,
            DateTimeHelper dateTimeHelper
        )
        {
            this.logger = logger;
            this.wagerRedoAllBetBLL = wagerRedoAllBetBLL;
            this.thirdPartyBLL = thirdPartyBLL;
            this.dateTimeHelper = dateTimeHelper;
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
            try
            {
                quick = 0;
                var dataTpTimer = thirdPartyBLL.GetThirdPartyTimer(tpId, tpType);
                DateTime timerTime;
                int last;
                if (dataTpTimer == null)
                {
                    timerTime = DateTime.Now.AddMinutes(-2 * intervalMins);
                    last = intervalMins;
                }
                else
                {
                    timerTime = dataTpTimer.StartTime.AddSeconds(1);
                    last = intervalMins;
                }

                var time = dateTimeHelper.GetTimeByHour(last, timeZone, "yyyy-MM-dd HH:mm:ss", timerTime);
                var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(timeZone, 0, 0)).ToUnixTimeSeconds();
                if (time.endUnix < cvTime)
                {
                    var result = await wagerRedoAllBetBLL.RedoWagersAsync(tpId, time.startTime, time.endTime, time.startUnix, time.endUnix);
                    if (result)
                    {
                        thirdPartyBLL.SetThirdPartyTimer(tpId, tpType, time.endUnix);
                    }
                }
                var ts = DateTime.Now - timerTime;
                var diffMin = (int)ts.TotalMinutes;
                if (diffMin >= 120)
                {
                    quick = 1;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
            finally
            {
                if (quick == 1)
                {
                    var dueTimeSpan = TimeSpan.FromSeconds(120);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
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
