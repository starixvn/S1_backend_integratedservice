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
    public class WagerOne789 : BackgroundService
    {
        private readonly ILogger<WagerOne789> logger;
        private readonly WagerOne789BLL wagerOne789BLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly DateTimeHelper dateTimeHelper;
        private readonly Log2File log2File;
        private Timer timerLT;
        private Timer timerLT_789;
        private Timer timerLC;
        private readonly int tpId = (int)Tp.One789;
        private readonly string _tpName = Tp.One789.ToString();
        private readonly int tpType = 1;
        private readonly int intervalMins = 3;
        private readonly int overlap = 5;
        private readonly int iRangMins = 20;
        private int quickLT = 0;
        private int quickLT_789 = 0;
        private int quickLC = 0;
        private readonly int timeZone = 0;

        public WagerOne789(
            ILogger<WagerOne789> logger,
            WagerOne789BLL wagerOne789BLL,
            ThirdPartyBLL thirdPartyBLL,
            DateTimeHelper dateTimeHelper
        )
        {
            this.logger = logger;
            this.wagerOne789BLL = wagerOne789BLL;
            this.thirdPartyBLL = thirdPartyBLL;
            this.dateTimeHelper = dateTimeHelper;
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
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
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
                    //if (time.endUnix < cvTime)
                    //{
                    var result = await wagerOne789BLL.GetWagersAsync(tpId, time.startTime, time.endTime, gameType);
                    if (result)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByGameType(tpId, tpType, time.endUnix, gameType);
                    }
                }
                var ts = DateTime.Now - timerTime;
                var diffMin = (int)ts.TotalMinutes;
                if (diffMin > 8)
                {
                    quickLT = 1;
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
            finally
            {
                if (quickLT == 1)
                {
                    var dueTimeSpan = TimeSpan.FromSeconds(45);
                    timerLT.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(30);
                    timerLT.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        private async void DoTaskLT_789(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
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
                    //if (time.endUnix < cvTime)
                    //{
                    var result = await wagerOne789BLL.GetWagersAsync(tpId, time.startTime, time.endTime, gameType);
                    if (result)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByGameType(tpId, tpType, time.endUnix, gameType);
                    }
                }
                var ts = DateTime.Now - timerTime;
                var diffMin = (int)ts.TotalMinutes;
                if (diffMin > 8)
                {
                    quickLT_789 = 1;
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
            finally
            {
                if (quickLT_789 == 1)
                {
                    var dueTimeSpan = TimeSpan.FromSeconds(45);
                    timerLT_789.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(8);
                    timerLT_789.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        private async void DoTaskLC(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
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
                    //if (time.endUnix < cvTime)
                    //{
                    var result = await wagerOne789BLL.GetWagersAsync(tpId, time.startTime, time.endTime, gameType);
                    if (result)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByGameType(tpId, tpType, time.endUnix, gameType);
                    }
                }
                var ts = DateTime.Now - timerTime;
                var diffMin = (int)ts.TotalMinutes;
                if (diffMin > 8)
                {
                    quickLC = 1;
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
            finally
            {
                if (quickLC == 1)
                {
                    var dueTimeSpan = TimeSpan.FromSeconds(45);
                    timerLC.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
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
