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
    public class WagerRedoPT : BackgroundService
    {
        private readonly ILogger<WagerRedoPT> logger;
        private readonly WagerPTBLL wagerPTBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly DateTimeHelper dateTimeHelper;
        private readonly IHostEnvironment environment;
        private readonly Log2File log2File;
        private Timer timerTHB;
        private Timer timerMYR;
        private Timer timerIDR;
        private Timer timerVNDGoal;
        private Timer timerVNDBwing;
        private Timer timerUSD;
        private Timer timerINR;
        private Timer timerCNY;
        private int intervalMins = 30;
        private int tpId = (int)Tp.PT;
        private int tpType = 2;
        private int beyond = 90;
        private string logName = Tp.PT.ToString();

        public WagerRedoPT(
            ILogger<WagerRedoPT> logger,
            WagerPTBLL wagerPTBLL,
            ThirdPartyBLL thirdPartyBLL,
            DateTimeHelper dateTimeHelper,
            IHostEnvironment environment,
            Log2File log2File
        )
        {
            this.logger = logger;
            this.wagerPTBLL = wagerPTBLL;
            this.thirdPartyBLL = thirdPartyBLL;
            this.dateTimeHelper = dateTimeHelper;
            this.environment = environment;
            this.log2File = log2File;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerMYR = new Timer(DoTaskMYR, null, TimeSpan.Zero, TimeSpan.Zero);
            timerTHB = new Timer(DoTaskTHB, null, TimeSpan.Zero, TimeSpan.Zero);
            timerIDR = new Timer(DoTaskIDR, null, TimeSpan.Zero, TimeSpan.Zero);
            timerVNDGoal = new Timer(DoTaskVNDGoal, null, TimeSpan.Zero, TimeSpan.Zero);
            timerVNDBwing = new Timer(DoTaskVNDBwing, null, TimeSpan.Zero, TimeSpan.Zero);
            timerUSD = new Timer(DoTaskUSD, null, TimeSpan.Zero, TimeSpan.Zero);
            timerINR = new Timer(DoTaskINR, null, TimeSpan.Zero, TimeSpan.Zero);
            timerCNY = new Timer(DoTaskCNY, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTaskTHB(object state)
        {
            if (!environment.IsProduction())
            {
                return;
            }
            int quick = 0;
            var currency = "THB";
            int siteId = 1;
            int gmt = 7;
            try
            {
                var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);
                DateTime timerTime;
                int last;
                last = intervalMins - 1;
                if (dataTpTimer == null)
                {
                    timerTime = DateTime.Now.AddMinutes(-4 * last);
                }
                else
                {
                    timerTime = dataTpTimer.StartTime;
                }

                //TimeSpan ts = DateTime.Now - timerTime;
                //int diffMin = (int)ts.TotalMinutes;

                //if (timerTime < DateTime.Now && diffMin > beyond)
                //{
                var time = dateTimeHelper.GetTimeByMinute(last, gmt, "yyyy-MM-dd HH:mm:ss", timerTime);
                var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(gmt, 0, 0)).ToUnixTimeSeconds();
                var diffMin = (cvTime - time.startUnix) / 60;
                if (time.endUnix < cvTime && diffMin > beyond)
                {
                    bool result = await wagerPTBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency, siteId, gmt);
                    if (result)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                    }
                }

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
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(90);
                    timerTHB.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timerTHB.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        private async void DoTaskMYR(object state)
        {
            if (!environment.IsProduction())
            {
                return;
            }
            int quick = 0;
            var currency = "MYR";
            int siteId = 1;
            int gmt = 8;
            try
            {
                var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);
                DateTime timerTime;
                int last;
                last = intervalMins - 1;
                if (dataTpTimer == null)
                {
                    timerTime = DateTime.Now.AddMinutes(-4 * last);
                }
                else
                {
                    timerTime = dataTpTimer.StartTime;
                }

                //TimeSpan ts = DateTime.Now - timerTime;
                //int diffMin = (int)ts.TotalMinutes;

                //if (timerTime < DateTime.Now && diffMin > beyond)
                //{
                var time = dateTimeHelper.GetTimeByMinute(last, gmt, "yyyy-MM-dd HH:mm:ss", timerTime);
                var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(gmt, 0, 0)).ToUnixTimeSeconds();
                var diffMin = (cvTime - time.startUnix) / 60;
                if (time.endUnix < cvTime && diffMin > beyond)
                {
                    bool result = await wagerPTBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency, siteId, gmt);
                    if (result)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                    }
                }

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
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(90);
                    timerMYR.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timerMYR.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        private async void DoTaskIDR(object state)
        {
            if (!environment.IsProduction())
            {
                return;
            }
            int quick = 0;
            var currency = "IDR";
            int siteId = 1;
            int gmt = 8;
            try
            {
                var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);
                DateTime timerTime;
                int last;
                last = intervalMins - 1;
                if (dataTpTimer == null)
                {
                    timerTime = DateTime.Now.AddMinutes(-4 * last);
                }
                else
                {
                    timerTime = dataTpTimer.StartTime;
                }

                //TimeSpan ts = DateTime.Now - timerTime;
                //int diffMin = (int)ts.TotalMinutes;

                //if (timerTime < DateTime.Now && diffMin > beyond)
                //{

                var time = dateTimeHelper.GetTimeByMinute(last, gmt, "yyyy-MM-dd HH:mm:ss", timerTime);
                var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(gmt, 0, 0)).ToUnixTimeSeconds();
                var diffMin = (cvTime - time.startUnix) / 60;
                if (time.endUnix < cvTime && diffMin > beyond)
                {
                    bool result = await wagerPTBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency, siteId, gmt);
                    if (result)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                    }
                }

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
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(90);
                    timerIDR.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timerIDR.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        private async void DoTaskVNDGoal(object state)
        {
            if (!environment.IsProduction())
            {
                return;
            }
            int quick = 0;
            var currency = "VND";
            int siteId = 2;
            int gmt = 7;
            try
            {
                var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency, siteId);
                DateTime timerTime;
                int last;
                last = intervalMins - 1;
                if (dataTpTimer == null)
                {
                    timerTime = DateTime.Now.AddMinutes(-4 * last);
                }
                else
                {
                    timerTime = dataTpTimer.StartTime;
                }

                //TimeSpan ts = DateTime.Now - timerTime;
                //int diffMin = (int)ts.TotalMinutes;

                //if (timerTime < DateTime.Now && diffMin > beyond)
                //{
                var time = dateTimeHelper.GetTimeByMinute(last, gmt, "yyyy-MM-dd HH:mm:ss", timerTime);
                var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(gmt, 0, 0)).ToUnixTimeSeconds();
                var diffMin = (cvTime - time.startUnix) / 60;
                if (time.endUnix < cvTime && diffMin > beyond)
                {
                    bool result = await wagerPTBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency, siteId, gmt);
                    if (result)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency, siteId);
                    }
                }

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
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(90);
                    timerVNDGoal.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timerVNDGoal.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        private async void DoTaskVNDBwing(object state)
        {
            if (!environment.IsProduction())
            {
                return;
            }
            int quick = 0;
            var currency = "VND";
            int siteId = 3;
            int gmt = 7;
            try
            {
                var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency, siteId);
                DateTime timerTime;
                int last;
                last = intervalMins - 1;
                if (dataTpTimer == null)
                {
                    timerTime = DateTime.Now.AddMinutes(-4 * last);
                }
                else
                {
                    timerTime = dataTpTimer.StartTime;
                }

                //TimeSpan ts = DateTime.Now - timerTime;
                //int diffMin = (int)ts.TotalMinutes;

                //if (timerTime < DateTime.Now && diffMin > beyond)
                //{
                var time = dateTimeHelper.GetTimeByMinute(last, gmt, "yyyy-MM-dd HH:mm:ss", timerTime);
                var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(gmt, 0, 0)).ToUnixTimeSeconds();
                var diffMin = (cvTime - time.startUnix) / 60;
                if (time.endUnix < cvTime && diffMin > beyond)
                {
                    bool result = await wagerPTBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency, siteId, gmt);
                    if (result)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency, siteId);
                    }
                }

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
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(90);
                    timerVNDBwing.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timerVNDBwing.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        private async void DoTaskUSD(object state)
        {
            if (!environment.IsProduction())
            {
                return;
            }
            int quick = 0;
            var currency = "USD";
            int siteId = 1;
            int gmt = 0;
            try
            {
                var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);
                DateTime timerTime;
                int last;
                last = intervalMins - 1;
                if (dataTpTimer == null)
                {
                    timerTime = DateTime.Now.AddMinutes(-4 * last);
                }
                else
                {
                    timerTime = dataTpTimer.StartTime;
                }

                //TimeSpan ts = DateTime.Now - timerTime;
                //int diffMin = (int)ts.TotalMinutes;

                //if (timerTime < DateTime.Now && diffMin > beyond)
                //{
                var time = dateTimeHelper.GetTimeByMinute(last, gmt, "yyyy-MM-dd HH:mm:ss", timerTime);
                var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(gmt, 0, 0)).ToUnixTimeSeconds();
                var diffMin = (cvTime - time.startUnix) / 60;
                if (time.endUnix < cvTime && diffMin > beyond)
                {
                    bool result = await wagerPTBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency, siteId, gmt);
                    if (result)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                    }
                }

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
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(90);
                    timerUSD.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timerUSD.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        private async void DoTaskINR(object state)
        {
            if (!environment.IsProduction())
            {
                return;
            }
            int quick = 0;
            var currency = "INR";
            int siteId = 1;
            int gmt = 5;
            int gmtMinute = 30;
            try
            {
                var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);
                DateTime timerTime;
                int last;
                last = intervalMins - 1;
                if (dataTpTimer == null)
                {
                    timerTime = DateTime.Now.AddMinutes(-4 * last);
                }
                else
                {
                    timerTime = dataTpTimer.StartTime;
                }

                //TimeSpan ts = DateTime.Now - timerTime;
                //int diffMin = (int)ts.TotalMinutes;

                //if (timerTime < DateTime.Now && diffMin > beyond)
                //{
                var time = dateTimeHelper.GetTimeByMinute(last, gmt, "yyyy-MM-dd HH:mm:ss", timerTime, gmtMinute);
                var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(gmt, 0, 0)).ToUnixTimeSeconds();
                var diffMin = (cvTime - time.startUnix) / 60;
                if (time.endUnix < cvTime && diffMin > beyond)
                {
                    bool result = await wagerPTBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency, siteId, gmt);
                    if (result)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                    }
                }

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
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(90);
                    timerINR.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timerINR.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        private async void DoTaskCNY(object state)
        {
            if (!environment.IsProduction())
            {
                return;
            }
            int quick = 0;
            var currency = "CNY";
            int siteId = 1;
            int gmt = 8;
            try
            {
                var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);
                DateTime timerTime;
                int last;
                last = intervalMins - 1;
                if (dataTpTimer == null)
                {
                    timerTime = DateTime.Now.AddMinutes(-4 * last);
                }
                else
                {
                    timerTime = dataTpTimer.StartTime;
                }

                //TimeSpan ts = DateTime.Now - timerTime;
                //int diffMin = (int)ts.TotalMinutes;

                //if (timerTime < DateTime.Now && diffMin > beyond)
                //{
                var time = dateTimeHelper.GetTimeByMinute(last, gmt, "yyyy-MM-dd HH:mm:ss", timerTime);
                var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(gmt, 0, 0)).ToUnixTimeSeconds();
                var diffMin = (cvTime - time.startUnix) / 60;
                if (time.endUnix < cvTime && diffMin > beyond)
                {
                    bool result = await wagerPTBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency, siteId, gmt);
                    if (result)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                    }
                }

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
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(90);
                    timerCNY.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timerCNY.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerTHB?.Change(Timeout.Infinite, 0);
            timerMYR?.Change(Timeout.Infinite, 0);
            timerIDR?.Change(Timeout.Infinite, 0);
            timerVNDGoal?.Change(Timeout.Infinite, 0);
            timerVNDBwing?.Change(Timeout.Infinite, 0);
            timerUSD?.Change(Timeout.Infinite, 0);
            timerINR?.Change(Timeout.Infinite, 0);
            timerCNY?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}