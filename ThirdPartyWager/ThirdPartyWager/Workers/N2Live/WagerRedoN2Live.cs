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
    public class WagerRedoN2Live : BackgroundService
    {
        private readonly ILogger<WagerRedoN2Live> logger;
        private readonly WagerN2LiveBLL wagerN2LiveBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly DateTimeHelper dateTimeHelper;
        private readonly Log2File log2File;
        private Timer timer;
        private int intervalDays = 1;
        private int iRangMins = 1440;
        private int tpId = (int)Tp.N2Live;
        private readonly string _tpName = Tp.N2Live.ToString();
        private int tpType = 2;
        private int quick = 0;
        private int iDays = 2;
        private readonly int timeZone = 0;

        public WagerRedoN2Live(
            ILogger<WagerRedoN2Live> logger,
            WagerN2LiveBLL wagerN2LiveBLL,
            ThirdPartyBLL thirdPartyBLL,
            DateTimeHelper dateTimeHelper
        )
        {
            this.logger = logger;
            this.wagerN2LiveBLL = wagerN2LiveBLL;
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
                string siteIds = "1,2,3";
                var site = siteIds.Split(",");
                foreach (var siteId in site)
                {
                    var setting = thirdPartyBLL.GetThirdPartyBySite(tpId, Convert.ToInt32(siteId));
                    var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByGameType(tpId, tpType, Convert.ToInt32(siteId));
                    DateTime timerTime;
                    
                    if (dataTpTimer == null)
                    {
                        timerTime = DateTime.Now.AddDays(-7);
                    }
                    else
                    {
                        timerTime = dataTpTimer.StartTime.AddSeconds(1);
                    }

                    var time = dateTimeHelper.GetTimeByDay(iRangMins, timeZone, "yyyy-MM-dd HH:mm:ss", timerTime);
                    var cvTime = DateTimeOffset.Now.AddDays(iDays * -1).ToOffset(new TimeSpan(timeZone, 0, 0)).ToUnixTimeSeconds();
                    if (time.endUnix < cvTime)
                    {
                        bool result = await wagerN2LiveBLL.GetWagersAsync(tpId, setting["ApiUrl"], setting["VendorId"], setting["MerchantPassCode"], time.startTime, time.endTime);
                        if (result)
                        {
                            thirdPartyBLL.SetThirdPartyTimerByGameType(tpId, tpType, time.endUnix, Convert.ToInt32(siteId));
                        }
                    }
                    int diffDay = (int)(DateTime.Now.Date - timerTime.Date).TotalDays;
                    if (diffDay >= (iDays + 1))
                    {
                        quick = 1;
                    }
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
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(60);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromDays(intervalDays);
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