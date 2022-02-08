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
    public class WagerSG : BackgroundService
    {
        private readonly ILogger<WagerSG> logger;
        private readonly WagerSGBLL wagerSGBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly DateTimeHelper dateTimeHelper;
        private Timer timer;
        private int iRangMins = 20;
        private int tpId = (int)Tp.SG;
        private readonly string _tpName = Tp.SG.ToString();
        private int tpType = 1;
        private int quick = 0;
        private readonly int timeZone = 8;

        public WagerSG(
            ILogger<WagerSG> logger,
            WagerSGBLL wagerSGBLL,
            ThirdPartyBLL thirdPartyBLL,
            DateTimeHelper dateTimeHelper
        )
        {
            this.logger = logger;
            this.wagerSGBLL = wagerSGBLL;
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
                    last = iRangMins - 1;
                    timerTime = DateTime.Now.AddMinutes(-1 * last);
                }
                else
                {
                    timerTime = dataTpTimer.StartTime.AddSeconds(-1);
                    last = iRangMins - 1;
                }
                if (timerTime < DateTime.Now)
                {
                    var time = dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyyMMdd'T'HHmmss", timerTime);
                    //var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(timeZone, 0, 0)).ToUnixTimeSeconds();
                    //if (time.endUnix < cvTime)
                    //{
                    bool result = await wagerSGBLL.GetWagersAsync(tpId, time.startTime, time.endTime);
                    if (result)
                    {
                        thirdPartyBLL.SetThirdPartyTimer(tpId, tpType, time.endUnix);
                    }
                }
                TimeSpan ts = DateTime.Now - timerTime;
                int diffMin = (int)ts.TotalMinutes;
                if (diffMin > 5)
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
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(35);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(45);
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
