using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerAG : BackgroundService
    {
        private readonly ILogger<WagerAG> logger;
        private readonly WagerAGBLL wagerAGBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly DateTimeHelper dateTimeHelper;
        private readonly IHostEnvironment environment;
        private Timer timer;
        private readonly int intervalMins = 3;
        private readonly int iRangMins = 10;
        private readonly int tpId = (int)Tp.AG;
        private readonly string _tpName = Tp.AG.ToString();
        private readonly int tpType = (int)WagerTimerType.Wager;
        private int quick = 0;
        private readonly int timeZone = -4;

        public WagerAG(
            ILogger<WagerAG> logger,
            WagerAGBLL wagerAGBLL,
            ThirdPartyBLL thirdPartyBLL,
            DateTimeHelper dateTimeHelper,
            IHostEnvironment environment
        )
        {
            this.logger = logger;
            this.wagerAGBLL = wagerAGBLL;
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
                    last = iRangMins - 1;
                    timerTime = DateTime.Now.AddMinutes(-1 * last);
                }
                else
                {
                    timerTime = dataTpTimer.StartTime;
                    last = iRangMins - 1;
                }
                if (timerTime < DateTime.Now)
                {
                    var time = dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyy-MM-dd HH:mm:ss", timerTime);
                    var result = await wagerAGBLL.GetWagersAsync(time.startTime, time.endTime);
                    if (result)
                    {
                        thirdPartyBLL.SetThirdPartyTimer(tpId, tpType, time.endUnix);
                    }
                }

                var ts = DateTime.Now - timerTime;
                var diffMin = (int)ts.TotalMinutes;
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
                    var dueTimeSpan = TimeSpan.FromSeconds(30);
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
