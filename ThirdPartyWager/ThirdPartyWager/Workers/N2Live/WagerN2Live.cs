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
    public class WagerN2Live : BackgroundService
    {
        private readonly ILogger<WagerN2Live> logger;
        private readonly WagerN2LiveBLL wagerN2LiveBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly DateTimeHelper dateTimeHelper;
        private readonly IHostEnvironment environment;
        private readonly Log2File log2File;
        private Timer timer;
        private int intervalMins = 2;
        private int iRangMins = 20;
        private int tpId = (int)Tp.N2Live;
        private int tpType = 1;
        private int quick = 0;
        private int beyond = 6;
        private string logName = Tp.N2Live.ToString();

        public WagerN2Live(
            ILogger<WagerN2Live> logger,
            WagerN2LiveBLL wagerN2LiveBLL,
            ThirdPartyBLL thirdPartyBLL,
            DateTimeHelper dateTimeHelper,
            IHostEnvironment environment,
            Log2File log2File
        )
        {
            this.logger = logger;
            this.wagerN2LiveBLL = wagerN2LiveBLL;
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
                quick = 0;
                string siteIds = "1,2,3";
                var site = siteIds.Split(",");
                foreach (var siteId in site)
                {
                    var setting = thirdPartyBLL.GetThirdPartyBySite(tpId, Convert.ToInt32(siteId));
                    var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByGameType(tpId, tpType, Convert.ToInt32(siteId));
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
                        var time = dateTimeHelper.GetTimeByMinute(last, 0, "yyyy-MM-dd HH:mm:ss", timerTime);
                        TimeSpan timeLimit = DateTime.UtcNow - DateTime.Parse(time.endTime);
                        if (timeLimit.TotalMinutes < 5)
                        {
                            // 日期值不能为未来日期 时间必须是当前时间的15分钟之前
                            time.endTime = DateTime.Parse(time.endTime).AddMinutes(-5).ToString("yyyy-MM-dd HH:mm:ss");
                            time.endUnix -= 300;
                        }
                        bool result = await wagerN2LiveBLL.GetWagersAsync(tpId, setting["ApiUrl"], setting["VendorId"], setting["MerchantPassCode"], time.startTime, time.endTime);
                        if (result)
                        {
                            thirdPartyBLL.SetThirdPartyTimerByGameType(tpId, tpType, time.endUnix, Convert.ToInt32(siteId));
                        }
                    }
                    TimeSpan ts = DateTime.Now - timerTime;
                    int diffMin = (int)ts.TotalMinutes;
                    if (diffMin > beyond)
                    {
                        quick = 1;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, logName);
                log2File.Error(ex.ToString(), logName);
            }
            finally
            {
                if (quick == 1)
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(45);
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