using Serilog.Context;
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
        private Timer timer;
        private readonly int intervalMins = 60;
        private readonly int tpId = (int)Tp.AG;
        private readonly string _tpName = Tp.AG.ToString();
        private readonly int tpType = (int)WagerTimerType.RedoWager;
        private int quick = 0;
        private readonly int timeZone = -4;

        public WagerRedoAG(
            ILogger<WagerRedoAG> logger,
            WagerRedoAGBLL wagerRedoAGBLL,
            ThirdPartyBLL thirdPartyBLL,
            DateTimeHelper dateTimeHelper,
            IHostEnvironment environment
        )
        {
            this.logger = logger;
            this.wagerRedoAGBLL = wagerRedoAGBLL;
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
            if (!environment.IsProduction())
            {
                return;
            }

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
                    timerTime = DateTime.Now.AddDays(-1);
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
                    var result = await wagerRedoAGBLL.RedoWagersAsync(time.startTime, time.endTime, time.startUnix, time.endUnix);
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
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
