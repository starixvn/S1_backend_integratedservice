using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;


namespace ThirdPartyWager.Workers
{
    public class WagerSkywind : BackgroundService
    {
        private readonly ILogger<WagerSkywind> logger;
        private readonly WagerSkywindBLL wagerSkywindBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly DateTimeHelper dateTimeHelper;
        private Timer timer;
        private readonly int intervalMins = 1;
        private readonly int iRangMins = 10;
        private readonly int tpId = (int)Tp.Skywind;
        private readonly string _tpName = Tp.Skywind.ToString();
        private readonly int tpType = (int)WagerTimerType.Wager;
        private int quick = 0;
        private readonly int timeZone = 0;

        public WagerSkywind(
            ILogger<WagerSkywind> logger,
            WagerSkywindBLL wagerSkywindBLL,
            ThirdPartyBLL thirdPartyBLL,
            DateTimeHelper dateTimeHelper
        )
        {
            this.logger = logger;
            this.wagerSkywindBLL = wagerSkywindBLL;
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
                List<int> siteIdList = new List<int> { 1, 2, 3 };
                var SetTimer = true;

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
                    var time = dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyy-MM-ddTHH:mm:ss.fffZ", timerTime);
                    foreach (var siteId in siteIdList)
                    {
                        var setting = thirdPartyBLL.GetThirdPartyBySite(tpId, siteId);
                        var result = await wagerSkywindBLL.GetWagersAsync(tpId, setting["ApiUrl"], setting["SecretKey"], setting["Username"], setting["Password"], time.startTime, time.endTime);
                        if (!result)
                        {
                            SetTimer = false;
                        }
                    }
                    if (SetTimer)
                    {
                        thirdPartyBLL.SetThirdPartyTimer(tpId, tpType, time.endUnix);
                    }
                }
                var ts = DateTime.Now - timerTime;
                var diffMin = (int)ts.TotalMinutes;
                if (diffMin >= 10)
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
                    var dueTimeSpan = TimeSpan.FromSeconds(45);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }
    }
}
