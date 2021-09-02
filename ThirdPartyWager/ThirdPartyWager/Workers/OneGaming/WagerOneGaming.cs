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
    public class WagerOneGaming : BackgroundService
    {
        private readonly ILogger<WagerOneGaming> logger;
        private readonly WagerOneGamingBLL wagerOneGamingBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly DateTimeHelper dateTimeHelper;
        private readonly IHostEnvironment environment;
        private readonly Log2File log2File;
        private Timer timerTHB;
        private Timer timerMYR;
        private Timer timerIDR;
        private Timer timerCNY;
        private Timer timerVND;
        private Timer timerUSD;
        private Timer timerINR;
        private int intervalMins = 3;
        private int iRangMins = 20;
        private int tpId = (int)Tp.OneGaming;
        private int tpType = 1;
        private int quickTHB = 0;
        private int quickMYR = 0;
        private int quickIDR = 0;
        private int quickCNY = 0;
        private int quickUSD = 0;
        private int beyond = 5;
        private readonly int timeZone = 8;
        private string logName = Tp.OneGaming.ToString();

        public WagerOneGaming(
            ILogger<WagerOneGaming> logger,
            WagerOneGamingBLL wagerOneGamingBLL,
            ThirdPartyBLL thirdPartyBLL,
            DateTimeHelper dateTimeHelper,
            IHostEnvironment environment,
            Log2File log2File
        )
        {
            this.logger = logger;
            this.wagerOneGamingBLL = wagerOneGamingBLL;
            this.thirdPartyBLL = thirdPartyBLL;
            this.dateTimeHelper = dateTimeHelper;
            this.environment = environment;
            this.log2File = log2File;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerTHB = new Timer(DoTaskTHB, null, TimeSpan.Zero, TimeSpan.Zero);
            timerMYR = new Timer(DoTaskMYR, null, TimeSpan.Zero, TimeSpan.Zero);
            timerIDR = new Timer(DoTaskIDR, null, TimeSpan.Zero, TimeSpan.Zero);
            timerCNY = new Timer(DoTaskCNY, null, TimeSpan.Zero, TimeSpan.Zero);
            timerVND = new Timer(DoTaskVND, null, TimeSpan.Zero, TimeSpan.Zero);
            timerUSD = new Timer(DoTaskUSD, null, TimeSpan.Zero, TimeSpan.Zero);
            timerINR = new Timer(DoTaskINR, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTaskTHB(object state)
        {
            if (environment.IsProduction())
            {
                try
                {
                    var currency = "THB";
                    quickTHB = 0;
                    var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);
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
                        var time = dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyyMMddHHmmss", timerTime);
                        //var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(timeZone, 0, 0)).ToUnixTimeSeconds();
                        //log2File.Error($"{logName}\r\n{time.endUnix}\r\n{cvTime}", "AA");
                        //if (time.endUnix < cvTime)
                        //{
                        bool result = await wagerOneGamingBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency);
                        if (result)
                        {
                            thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                        }
                    }
                    TimeSpan ts = DateTime.Now - timerTime;
                    int diffMin = (int)ts.TotalMinutes;
                    if (diffMin > beyond)
                    {
                        quickTHB = 1;
                    }

                }
                catch (Exception ex)
                {
                    logger.LogError(ex, logName);
                    log2File.Error(ex.ToString(), logName);
                }
                finally
                {
                    if (quickTHB == 1)
                    {
                        TimeSpan dueTimeSpan = TimeSpan.FromSeconds(45);
                        timerTHB.Change(dueTimeSpan, TimeSpan.Zero);
                    }
                    else
                    {
                        TimeSpan dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                        timerTHB.Change(dueTimeSpan, TimeSpan.Zero);
                    }
                }
            }
        }

        private async void DoTaskMYR(object state)
        {
            if (environment.IsProduction())
            {
                try
                {
                    var currency = "MYR";
                    quickMYR = 0;
                    var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);
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
                        var time = dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyyMMddHHmmss", timerTime);
                        //var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(timeZone, 0, 0)).ToUnixTimeSeconds();
                        //log2File.Error($"{logName}\r\n{time.endUnix}\r\n{cvTime}", "AA");
                        //if (time.endUnix < cvTime)
                        //{
                        bool result = await wagerOneGamingBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency);
                        if (result)
                        {
                            thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                        }
                    }
                    TimeSpan ts = DateTime.Now - timerTime;
                    int diffMin = (int)ts.TotalMinutes;
                    if (diffMin > beyond)
                    {
                        quickMYR = 1;
                    }

                }
                catch (Exception ex)
                {
                    logger.LogError(ex, logName);
                    log2File.Error(ex.ToString(), logName);
                }
                finally
                {
                    if (quickMYR == 1)
                    {
                        TimeSpan dueTimeSpan = TimeSpan.FromSeconds(45);
                        timerMYR.Change(dueTimeSpan, TimeSpan.Zero);
                    }
                    else
                    {
                        TimeSpan dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                        timerMYR.Change(dueTimeSpan, TimeSpan.Zero);
                    }
                }
            }
        }

        private async void DoTaskIDR(object state)
        {
            try
            {
                var currency = "IDR";
                quickIDR = 0;
                var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);
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
                    var time = dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyyMMddHHmmss", timerTime);
                    //var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(timeZone, 0, 0)).ToUnixTimeSeconds();
                    //log2File.Error($"{logName}\r\n{time.endUnix}\r\n{cvTime}", "AA");
                    //if (time.endUnix < cvTime)
                    //{
                    bool result = await wagerOneGamingBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency);
                    if (result)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                    }
                }
                TimeSpan ts = DateTime.Now - timerTime;
                int diffMin = (int)ts.TotalMinutes;
                if (diffMin > beyond)
                {
                    quickIDR = 1;
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, logName);
                log2File.Error(ex.ToString(), logName);
            }
            finally
            {
                if (quickIDR == 1)
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(45);
                    timerIDR.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timerIDR.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        private async void DoTaskCNY(object state)
        {
            if (environment.IsProduction())
            {
                try
                {
                    var currency = "CNY";
                    quickCNY = 0;
                    var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);
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
                        var time = dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyyMMddHHmmss", timerTime);
                        //var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(timeZone, 0, 0)).ToUnixTimeSeconds();
                        //log2File.Error($"{logName}\r\n{time.endUnix}\r\n{cvTime}", "AA");
                        //if (time.endUnix < cvTime)
                        //{
                        bool result = await wagerOneGamingBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency);
                        if (result)
                        {
                            thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                        }
                    }
                    TimeSpan ts = DateTime.Now - timerTime;
                    int diffMin = (int)ts.TotalMinutes;
                    if (diffMin > beyond)
                    {
                        quickCNY = 1;
                    }

                }
                catch (Exception ex)
                {
                    logger.LogError(ex, logName);
                    log2File.Error(ex.ToString(), logName);
                }
                finally
                {
                    if (quickCNY == 1)
                    {
                        TimeSpan dueTimeSpan = TimeSpan.FromSeconds(45);
                        timerCNY.Change(dueTimeSpan, TimeSpan.Zero);
                    }
                    else
                    {
                        TimeSpan dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                        timerCNY.Change(dueTimeSpan, TimeSpan.Zero);
                    }
                }
            }
        }

        private async void DoTaskVND(object state)
        {
            int quick = 0;
            if (environment.IsProduction())
            {
                try
                {
                    var currency = "VND";

                    var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);
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
                        var time = dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyyMMddHHmmss", timerTime);
                        //var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(timeZone, 0, 0)).ToUnixTimeSeconds();
                        //log2File.Error($"{logName}\r\n{time.endUnix}\r\n{cvTime}", "AA");
                        //if (time.endUnix < cvTime)
                        //{
                        bool result = await wagerOneGamingBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency);
                        if (result)
                        {
                            thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                        }
                    }
                    TimeSpan ts = DateTime.Now - timerTime;
                    int diffMin = (int)ts.TotalMinutes;
                    if (diffMin > beyond)
                    {
                        quick = 1;
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
                        timerVND.Change(dueTimeSpan, TimeSpan.Zero);
                    }
                    else
                    {
                        TimeSpan dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                        timerVND.Change(dueTimeSpan, TimeSpan.Zero);
                    }
                }
            }
        }

        private async void DoTaskUSD(object state)
        {
            if (environment.IsProduction())
            {
                try
                {
                    var currency = "USD";
                    quickUSD = 0;
                    var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);
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
                        var time = dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyyMMddHHmmss", timerTime);
                        //var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(timeZone, 0, 0)).ToUnixTimeSeconds();
                        //log2File.Error($"{logName}\r\n{time.endUnix}\r\n{cvTime}", "AA");
                        //if (time.endUnix < cvTime)
                        //{
                        bool result = await wagerOneGamingBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency);
                        if (result)
                        {
                            thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                        }
                    }
                    TimeSpan ts = DateTime.Now - timerTime;
                    int diffMin = (int)ts.TotalMinutes;
                    if (diffMin > beyond)
                    {
                        quickUSD = 1;
                    }

                }
                catch (Exception ex)
                {
                    logger.LogError(ex, logName);
                    log2File.Error(ex.ToString(), logName);
                }
                finally
                {
                    if (quickUSD == 1)
                    {
                        TimeSpan dueTimeSpan = TimeSpan.FromSeconds(45);
                        timerUSD.Change(dueTimeSpan, TimeSpan.Zero);
                    }
                    else
                    {
                        TimeSpan dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                        timerUSD.Change(dueTimeSpan, TimeSpan.Zero);
                    }
                }
            }
        }

        private async void DoTaskINR(object state)
        {
            int quick = 0;
            if (environment.IsProduction())
            {
                try
                {
                    var currency = "INR";
                    var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);
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
                        var time = dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyyMMddHHmmss", timerTime);
                        //var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(timeZone, 0, 0)).ToUnixTimeSeconds();
                        //log2File.Error($"{logName}\r\n{time.endUnix}\r\n{cvTime}", "AA");
                        //if (time.endUnix < cvTime)
                        //{
                        bool result = await wagerOneGamingBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency);
                        if (result)
                        {
                            thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                        }
                    }
                    TimeSpan ts = DateTime.Now - timerTime;
                    int diffMin = (int)ts.TotalMinutes;
                    if (diffMin > beyond)
                    {
                        quick = 1;
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
                        timerINR.Change(dueTimeSpan, TimeSpan.Zero);
                    }
                    else
                    {
                        TimeSpan dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                        timerINR.Change(dueTimeSpan, TimeSpan.Zero);
                    }
                }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerTHB?.Change(Timeout.Infinite, 0);
            timerMYR?.Change(Timeout.Infinite, 0);
            timerIDR?.Change(Timeout.Infinite, 0);
            timerCNY?.Change(Timeout.Infinite, 0);
            timerVND?.Change(Timeout.Infinite, 0);
            timerUSD?.Change(Timeout.Infinite, 0);
            timerINR?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
