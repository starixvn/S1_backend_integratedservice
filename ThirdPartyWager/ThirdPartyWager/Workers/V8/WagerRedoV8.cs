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
    public class WagerRedoV8 : BackgroundService
    {
        private readonly ILogger<WagerRedoV8> logger;
        private readonly WagerV8BLL wagerV8BLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly DateTimeHelper dateTimeHelper;
        private Timer timer;
        private int intervalMins = 60;
        private int tpId = (int)Tp.V8;
        private readonly string _tpName = Tp.V8.ToString();
        private int tpType = 2;
        private int quick = 0;
        private int beyond = 120;
        private readonly int timeZone = 0;

        public WagerRedoV8(
            ILogger<WagerRedoV8> logger,
            WagerV8BLL wagerV8BLL,
            ThirdPartyBLL thirdPartyBLL,
            DateTimeHelper dateTimeHelper
        )
        {
            this.logger = logger;
            this.wagerV8BLL = wagerV8BLL;
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

                var time = dateTimeHelper.GetTimeByHour(last, timeZone, "yyyy-MM-dd HH:mm:ss", timerTime);
                var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(timeZone, 0, 0)).ToUnixTimeSeconds();
                if (time.endUnix < cvTime)
                {
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
                logger.LogError(ex, ex.Message);
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