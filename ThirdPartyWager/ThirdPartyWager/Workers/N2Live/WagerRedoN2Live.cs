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
    public class WagerRedoN2Live : BackgroundService
    {
        private readonly ILogger<WagerRedoN2Live> logger;
        private readonly WagerN2LiveBLL wagerN2LiveBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly DateTimeHelper dateTimeHelper;
        private readonly IHostEnvironment environment;
        private readonly Log2File log2File;
        private Timer timer;
        private int intervalDays = 1;
        private int iRangMins = 1440;
        private int tpId = (int)Tp.N2Live;
        private int tpType = 2;
        private int quick = 0;
        private int iDays = 2;
        private string logName = Tp.N2Live.ToString();

        public WagerRedoN2Live(
            ILogger<WagerRedoN2Live> logger,
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
                    
                    if (dataTpTimer == null)
                    {
                        timerTime = DateTime.Now.AddDays(-7);
                    }
                    else
                    {
                        timerTime = dataTpTimer.StartTime.AddSeconds(1);
                    }

                    if (timerTime < DateTime.Now.AddDays(iDays * -1))
                    {
                        var time = dateTimeHelper.GetTimeByDay(iRangMins, 0, "yyyy-MM-dd HH:mm:ss", timerTime);
                        
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
                logger.LogError(ex.Message);
                log2File.Error(ex.ToString(), logName);
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