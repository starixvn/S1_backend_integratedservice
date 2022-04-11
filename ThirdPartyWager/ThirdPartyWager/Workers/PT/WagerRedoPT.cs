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
    public class WagerRedoPT : BackgroundService
    {
        private readonly ILogger<WagerRedoPT> logger;
        private readonly WagerPTBLL wagerPTBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly DateTimeHelper dateTimeHelper;
        private readonly IHostEnvironment environment;
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
        private readonly string _tpName = Tp.PT.ToString();
        private int tpType = 2;
        private int beyond = 90;

        public WagerRedoPT(
            ILogger<WagerRedoPT> logger,
            WagerPTBLL wagerPTBLL,
            ThirdPartyBLL thirdPartyBLL,
            DateTimeHelper dateTimeHelper,
            IHostEnvironment environment
        )
        {
            this.logger = logger;
            this.wagerPTBLL = wagerPTBLL;
            this.thirdPartyBLL = thirdPartyBLL;
            this.dateTimeHelper = dateTimeHelper;
            this.environment = environment;
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
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            if (!environment.IsProduction())
            {
                return;
            }
            var quick = 0;
            var currency = "THB";
            var siteId = 1;
            var gmt = 7;
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
                    var result = await wagerPTBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency, siteId, gmt);
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
                logger.LogError(ex, ex.Message);
            }
            finally
            {
                if (quick == 1)
                {
                    var dueTimeSpan = TimeSpan.FromSeconds(90);
                    timerTHB.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timerTHB.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        private async void DoTaskMYR(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            if (!environment.IsProduction())
            {
                return;
            }
            var quick = 0;
            var currency = "MYR";
            var siteId = 1;
            var gmt = 8;
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
                    var result = await wagerPTBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency, siteId, gmt);
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
                logger.LogError(ex, ex.Message);
            }
            finally
            {
                if (quick == 1)
                {
                    var dueTimeSpan = TimeSpan.FromSeconds(90);
                    timerMYR.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timerMYR.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        private async void DoTaskIDR(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            if (!environment.IsProduction())
            {
                return;
            }
            var quick = 0;
            var currency = "IDR";
            var siteId = 1;
            var gmt = 8;
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
                    var result = await wagerPTBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency, siteId, gmt);
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
                logger.LogError(ex, ex.Message);
            }
            finally
            {
                if (quick == 1)
                {
                    var dueTimeSpan = TimeSpan.FromSeconds(90);
                    timerIDR.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timerIDR.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        private async void DoTaskVNDGoal(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            if (!environment.IsProduction())
            {
                return;
            }
            var quick = 0;
            var currency = "VND";
            var siteId = 2;
            var gmt = 7;
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
                    var result = await wagerPTBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency, siteId, gmt);
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
                logger.LogError(ex, ex.Message);
            }
            finally
            {
                if (quick == 1)
                {
                    var dueTimeSpan = TimeSpan.FromSeconds(90);
                    timerVNDGoal.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timerVNDGoal.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        private async void DoTaskVNDBwing(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            if (!environment.IsProduction())
            {
                return;
            }
            var quick = 0;
            var currency = "VND";
            var siteId = 3;
            var gmt = 7;
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
                    var result = await wagerPTBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency, siteId, gmt);
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
                logger.LogError(ex, ex.Message);
            }
            finally
            {
                if (quick == 1)
                {
                    var dueTimeSpan = TimeSpan.FromSeconds(90);
                    timerVNDBwing.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timerVNDBwing.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        private async void DoTaskUSD(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            if (!environment.IsProduction())
            {
                return;
            }
            var quick = 0;
            var currency = "USD";
            var siteId = 1;
            var gmt = 0;
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
                    var result = await wagerPTBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency, siteId, gmt);
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
                logger.LogError(ex, ex.Message);
            }
            finally
            {
                if (quick == 1)
                {
                    var dueTimeSpan = TimeSpan.FromSeconds(90);
                    timerUSD.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timerUSD.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        private async void DoTaskINR(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            if (!environment.IsProduction())
            {
                return;
            }
            var quick = 0;
            var currency = "INR";
            var siteId = 1;
            var gmt = 5;
            var gmtMinute = 30;
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
                    var result = await wagerPTBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency, siteId, gmt, gmtMinute);
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
                logger.LogError(ex, ex.Message);
            }
            finally
            {
                if (quick == 1)
                {
                    var dueTimeSpan = TimeSpan.FromSeconds(90);
                    timerINR.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timerINR.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        private async void DoTaskCNY(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            if (!environment.IsProduction())
            {
                return;
            }
            var quick = 0;
            var currency = "CNY";
            var siteId = 1;
            var gmt = 8;
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
                    var result = await wagerPTBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency, siteId, gmt);
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
                logger.LogError(ex, ex.Message);
            }
            finally
            {
                if (quick == 1)
                {
                    var dueTimeSpan = TimeSpan.FromSeconds(90);
                    timerCNY.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
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