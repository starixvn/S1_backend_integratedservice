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
    public class WagerRedoOneGaming : BackgroundService
    {
        private readonly ILogger<WagerRedoOneGaming> logger;
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
        private int intervalMins = 60;
        private int tpId = (int)Tp.OneGaming;
        private int tpType = 2;
        private int beyond = 120;

        public WagerRedoOneGaming(
            ILogger<WagerRedoOneGaming> logger,
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
            return Task.CompletedTask;
        }

        private async void DoTaskTHB(object state)
        {
            if (environment.IsProduction())
            {
                int quick = 0;
                try
                {
                    var currency = "THB";
                    var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);
                    DateTime timerTime;
                    int last;
                    int first = 0;
                    if (dataTpTimer == null)
                    {
                        first = 1;
                        last = intervalMins;
                        timerTime = DateTime.Now.AddMinutes(-1 * last);
                    }
                    else
                    {
                        timerTime = dataTpTimer.StartTime.AddMinutes(1);
                        last = intervalMins;
                    }

                    if (timerTime < DateTime.Now)
                    {
                        var time = dateTimeHelper.GetTimeByHour(last, 8, "yyyyMMddHHmmss", timerTime);
                        bool result = await wagerOneGamingBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency);
                        if (result == true)
                        {
                            thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                        }
                        else
                        {
                            if (first == 1)
                            {
                                quick = first;
                            }
                        }
                    }
                    TimeSpan ts = DateTime.Now - timerTime;
                    int diffMin = (int)ts.TotalMinutes;
                    if (diffMin >= beyond)
                    {
                        quick = 1;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                    log2File.Error(ex.ToString(), "WagerRedoOneGaming");
                }
                finally
                {
                    if (quick == 1)
                    {
                        TimeSpan dueTimeSpan = TimeSpan.FromSeconds(beyond);
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
                int quick = 0;
                try
                {
                    var currency = "MYR";
                    var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);
                    DateTime timerTime;
                    int last;
                    int first = 0;
                    if (dataTpTimer == null)
                    {
                        first = 1;
                        last = intervalMins;
                        timerTime = DateTime.Now.AddMinutes(-1 * last);
                    }
                    else
                    {
                        timerTime = dataTpTimer.StartTime.AddMinutes(1);
                        last = intervalMins;
                    }

                    if (timerTime < DateTime.Now)
                    {
                        var time = dateTimeHelper.GetTimeByHour(last, 8, "yyyyMMddHHmmss", timerTime);
                        bool result = await wagerOneGamingBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency);
                        if (result == true)
                        {
                            thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                        }
                        else
                        {
                            if (first == 1)
                            {
                                quick = first;
                            }
                        }
                    }
                    TimeSpan ts = DateTime.Now - timerTime;
                    int diffMin = (int)ts.TotalMinutes;
                    if (diffMin >= beyond)
                    {
                        quick = 1;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                    log2File.Error(ex.ToString(), "WagerRedoOneGaming");
                }
                finally
                {
                    if (quick == 1)
                    {
                        TimeSpan dueTimeSpan = TimeSpan.FromSeconds(beyond);
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
            int quick = 0;
            try
            {
                var currency = "IDR";
                var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);
                DateTime timerTime;
                int last;
                int first = 0;
                if (dataTpTimer == null)
                {
                    first = 1;
                    last = intervalMins;
                    timerTime = DateTime.Now.AddMinutes(-1 * last);
                }
                else
                {
                    timerTime = dataTpTimer.StartTime.AddMinutes(1);
                    last = intervalMins;
                }

                if (timerTime < DateTime.Now)
                {
                    var time = dateTimeHelper.GetTimeByHour(last, 8, "yyyyMMddHHmmss", timerTime);
                    bool result = await wagerOneGamingBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency);
                    if (result == true)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                    }
                    else
                    {
                        if (first == 1)
                        {
                            quick = first;
                        }
                    }
                }
                TimeSpan ts = DateTime.Now - timerTime;
                int diffMin = (int)ts.TotalMinutes;
                if (diffMin >= beyond)
                {
                    quick = 1;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                log2File.Error(ex.ToString(), "WagerRedoOneGaming");
            }
            finally
            {
                if (quick == 1)
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(beyond);
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
            int quick = 0;
            if (environment.IsProduction())
            {
                try
                {
                    var currency = "CNY";
                    var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);
                    DateTime timerTime;
                    int last;
                    int first = 0;
                    if (dataTpTimer == null)
                    {
                        first = 1;
                        last = intervalMins;
                        timerTime = DateTime.Now.AddMinutes(-1 * last);
                    }
                    else
                    {
                        timerTime = dataTpTimer.StartTime.AddMinutes(1);
                        last = intervalMins;
                    }

                    if (timerTime < DateTime.Now)
                    {
                        var time = dateTimeHelper.GetTimeByHour(last, 8, "yyyyMMddHHmmss", timerTime);
                        bool result = await wagerOneGamingBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency);
                        if (result == true)
                        {
                            thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                        }
                        else
                        {
                            if (first == 1)
                            {
                                quick = first;
                            }
                        }
                    }
                    TimeSpan ts = DateTime.Now - timerTime;
                    int diffMin = (int)ts.TotalMinutes;
                    if (diffMin >= beyond)
                    {
                        quick = 1;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                    log2File.Error(ex.ToString(), "WagerRedoOneGaming");
                }
                finally
                {
                    if (quick == 1)
                    {
                        TimeSpan dueTimeSpan = TimeSpan.FromSeconds(beyond);
                        timerIDR.Change(dueTimeSpan, TimeSpan.Zero);
                    }
                    else
                    {
                        TimeSpan dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                        timerIDR.Change(dueTimeSpan, TimeSpan.Zero);
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
                    int first = 0;
                    if (dataTpTimer == null)
                    {
                        first = 1;
                        last = intervalMins;
                        timerTime = DateTime.Now.AddMinutes(-1 * last);
                    }
                    else
                    {
                        timerTime = dataTpTimer.StartTime.AddMinutes(1);
                        last = intervalMins;
                    }

                    if (timerTime < DateTime.Now)
                    {
                        var time = dateTimeHelper.GetTimeByHour(last, 8, "yyyyMMddHHmmss", timerTime);
                        bool result = await wagerOneGamingBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency);
                        if (result == true)
                        {
                            thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                        }
                        else
                        {
                            if (first == 1)
                            {
                                quick = first;
                            }
                        }
                    }
                    TimeSpan ts = DateTime.Now - timerTime;
                    int diffMin = (int)ts.TotalMinutes;
                    if (diffMin >= beyond)
                    {
                        quick = 1;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                    log2File.Error(ex.ToString(), "WagerRedoOneGaming");
                }
                finally
                {
                    if (quick == 1)
                    {
                        TimeSpan dueTimeSpan = TimeSpan.FromSeconds(beyond);
                        timerIDR.Change(dueTimeSpan, TimeSpan.Zero);
                    }
                    else
                    {
                        TimeSpan dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                        timerIDR.Change(dueTimeSpan, TimeSpan.Zero);
                    }
                }
            }
        }

        private async void DoTaskUSD(object state)
        {
            int quick = 0;
            if (environment.IsProduction())
            {
                try
                {
                    var currency = "USD";
                    var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);
                    DateTime timerTime;
                    int last;
                    int first = 0;
                    if (dataTpTimer == null)
                    {
                        first = 1;
                        last = intervalMins;
                        timerTime = DateTime.Now.AddMinutes(-1 * last);
                    }
                    else
                    {
                        timerTime = dataTpTimer.StartTime.AddMinutes(1);
                        last = intervalMins;
                    }

                    if (timerTime < DateTime.Now)
                    {
                        var time = dateTimeHelper.GetTimeByHour(last, 8, "yyyyMMddHHmmss", timerTime);
                        bool result = await wagerOneGamingBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency);
                        if (result == true)
                        {
                            thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                        }
                        else
                        {
                            if (first == 1)
                            {
                                quick = first;
                            }
                        }
                    }
                    TimeSpan ts = DateTime.Now - timerTime;
                    int diffMin = (int)ts.TotalMinutes;
                    if (diffMin >= beyond)
                    {
                        quick = 1;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                    log2File.Error(ex.ToString(), "WagerRedoOneGaming");
                }
                finally
                {
                    if (quick == 1)
                    {
                        TimeSpan dueTimeSpan = TimeSpan.FromSeconds(beyond);
                        timerIDR.Change(dueTimeSpan, TimeSpan.Zero);
                    }
                    else
                    {
                        TimeSpan dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                        timerIDR.Change(dueTimeSpan, TimeSpan.Zero);
                    }
                }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            //WriteToFile("WriteToFileHostedService: Process Stopped");
            timerTHB?.Change(Timeout.Infinite, 0);
            timerMYR?.Change(Timeout.Infinite, 0);
            timerIDR?.Change(Timeout.Infinite, 0);
            timerCNY?.Change(Timeout.Infinite, 0);
            timerVND?.Change(Timeout.Infinite, 0);
            timerUSD?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
