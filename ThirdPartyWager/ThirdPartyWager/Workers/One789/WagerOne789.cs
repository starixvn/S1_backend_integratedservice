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
    public class WagerOne789 : BackgroundService
    {
        private readonly ILogger<WagerOne789> logger;
        private readonly WagerOne789BLL wagerOne789BLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly DateTimeHelper dateTimeHelper;
        private readonly IHostEnvironment environment;
        private readonly Log2File log2File;
        private Timer timerLT;
        private Timer timerLT_789;
        private Timer timerLC;
        private int tpId = (int)Tp.One789;
        private int tpType = 1;
        private int intervalMins = 3;
        private int overlap = 5;
        private int iRangMins = 20;
        private int quickLT = 0;
        private int quickLT_789 = 0;
        private int quickLC = 0;
        private readonly int timeZone = 0;
        private string logName = Tp.One789.ToString();

        public WagerOne789(
            ILogger<WagerOne789> logger,
            WagerOne789BLL wagerOne789BLL,
            ThirdPartyBLL thirdPartyBLL,
            DateTimeHelper dateTimeHelper,
            IHostEnvironment environment,
            Log2File log2File
        )
        {
            this.logger = logger;
            this.wagerOne789BLL = wagerOne789BLL;
            this.thirdPartyBLL = thirdPartyBLL;
            this.dateTimeHelper = dateTimeHelper;
            this.environment = environment;
            this.log2File = log2File;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerLT = new Timer(DoTaskLT, null, TimeSpan.Zero, TimeSpan.Zero);
            timerLT_789 = new Timer(DoTaskLT_789, null, TimeSpan.Zero, TimeSpan.Zero);
            timerLC = new Timer(DoTaskLC, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTaskLT(object state)
        {
            var gameType = (int)GameType.One789_Lottery;
            try
            {
                quickLT = 0;
                var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByGameType(tpId, tpType, gameType);
                DateTime timerTime;
                int last;
                if (dataTpTimer == null)
                {
                    last = iRangMins - 1;
                    timerTime = DateTime.Now.AddMinutes(-1 * last);
                }
                else
                {
                    timerTime = dataTpTimer.StartTime.AddMinutes(-1 * overlap);
                    last = 45;
                }
                if (timerTime < DateTime.Now)
                {
                    var time = dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyy-MM-ddTHH:mm:ssZ", timerTime);
                    //var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(timeZone, 0, 0)).ToUnixTimeSeconds();
                    //log2File.Error($"{logName}\r\n{time.endUnix}\r\n{cvTime}", "AA");
                    //if (time.endUnix < cvTime)
                    //{
                    bool result = await wagerOne789BLL.GetWagersAsync(tpId, time.startTime, time.endTime, gameType);
                    if (result)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByGameType(tpId, tpType, time.endUnix, gameType);
                    }
                }
                TimeSpan ts = DateTime.Now - timerTime;
                int diffMin = (int)ts.TotalMinutes;
                if (diffMin > 8)
                {
                    quickLT = 1;
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, logName);
                log2File.Error(ex.ToString(), logName);
            }
            finally
            {
                if (quickLT == 1)
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(45);
                    timerLT.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromMinutes(30);
                    timerLT.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        private async void DoTaskLT_789(object state)
        {
            var gameType = (int)GameType.One789_Lottery_789;
            try
            {
                quickLT_789 = 0;
                var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByGameType(tpId, tpType, gameType);
                DateTime timerTime;
                int last;
                if (dataTpTimer == null)
                {
                    last = iRangMins - 1;
                    timerTime = DateTime.Now.AddMinutes(-1 * last);
                }
                else
                {
                    timerTime = dataTpTimer.StartTime.AddMinutes(-1 * overlap);
                    last = iRangMins - 1;
                }
                if (timerTime < DateTime.Now)
                {
                    var time = dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyy-MM-dd HH:mm:ssZ", timerTime);
                    //var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(timeZone, 0, 0)).ToUnixTimeSeconds();
                    //log2File.Error($"{logName}\r\n{time.endUnix}\r\n{cvTime}", "AA");
                    //if (time.endUnix < cvTime)
                    //{
                    bool result = await wagerOne789BLL.GetWagersAsync(tpId, time.startTime, time.endTime, gameType);
                    if (result)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByGameType(tpId, tpType, time.endUnix, gameType);
                    }
                }
                TimeSpan ts = DateTime.Now - timerTime;
                int diffMin = (int)ts.TotalMinutes;
                if (diffMin > 8)
                {
                    quickLT_789 = 1;
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, logName);
                log2File.Error(ex.ToString(), logName);
            }
            finally
            {
                if (quickLT_789 == 1)
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(45);
                    timerLT_789.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromMinutes(8);
                    timerLT_789.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        private async void DoTaskLC(object state)
        {
            var gameType = (int)GameType.One789_LiveCasino;
            try
            {
                quickLC = 0;
                var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByGameType(tpId, tpType, gameType);
                DateTime timerTime;
                int last;
                if (dataTpTimer == null)
                {
                    last = iRangMins - 1;
                    timerTime = DateTime.Now.AddMinutes(-1 * last);
                }
                else
                {
                    timerTime = dataTpTimer.StartTime.AddMinutes(-1 * overlap);
                    last = iRangMins - 1;
                }
                if (timerTime < DateTime.Now)
                {
                    var time = dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyy-MM-dd HH:mm:ssZ", timerTime);
                    //var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(timeZone, 0, 0)).ToUnixTimeSeconds();
                    //log2File.Error($"{logName}\r\n{time.endUnix}\r\n{cvTime}", "AA");
                    //if (time.endUnix < cvTime)
                    //{
                    bool result = await wagerOne789BLL.GetWagersAsync(tpId, time.startTime, time.endTime, gameType);
                    if (result)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByGameType(tpId, tpType, time.endUnix, gameType);
                    }
                }
                TimeSpan ts = DateTime.Now - timerTime;
                int diffMin = (int)ts.TotalMinutes;
                if (diffMin > 8)
                {
                    quickLC = 1;
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, logName);
                log2File.Error(ex.ToString(), logName);
            }
            finally
            {
                if (quickLC == 1)
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(45);
                    timerLC.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timerLC.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            //WriteToFile("WriteToFileHostedService: Process Stopped");
            timerLT?.Change(Timeout.Infinite, 0);
            timerLT_789?.Change(Timeout.Infinite, 0);
            timerLC?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
