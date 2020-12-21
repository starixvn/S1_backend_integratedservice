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

    public class WagerIconic : BackgroundService
    {
        private readonly ILogger<WagerIconic> logger;
        private readonly WagerIconicBLL wagerIconicBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly DateTimeHelper dateTimeHelper;
        private readonly IHostEnvironment environment;
        private Timer timerTHB;
        private Timer timerMYR;
        private Timer timerIDR;
        private Timer timerVND;
        private Timer timerCNY;
        private Timer timerUSDT;
        private int intervalMins = 3;
        private int iRangMins = 20;
        private int tpId = (int)Tp.Iconic;
        private int tpType = 1;
        private int quickTHB = 0;
        private int quickMYR = 0;
        private int quickIDR = 0;
        private int quickVND = 0;
        private int quickCNY = 0;
        private int quickUSDT = 0;
        private int beyond = 5;

        public WagerIconic(
            ILogger<WagerIconic> logger,
            WagerIconicBLL wagerIconicBLL,
            ThirdPartyBLL thirdPartyBLL,
            DateTimeHelper dateTimeHelper,
            IHostEnvironment environment
        )
        {
            this.logger = logger;
            this.wagerIconicBLL = wagerIconicBLL;
            this.thirdPartyBLL = thirdPartyBLL;
            this.dateTimeHelper = dateTimeHelper;
            this.environment = environment;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerTHB = new Timer(DoTaskTHB, null, TimeSpan.Zero, TimeSpan.Zero);
            timerMYR = new Timer(DoTaskMYR, null, TimeSpan.Zero, TimeSpan.Zero);
            timerIDR = new Timer(DoTaskIDR, null, TimeSpan.Zero, TimeSpan.Zero);
            timerVND = new Timer(DoTaskVND, null, TimeSpan.Zero, TimeSpan.Zero);
            timerCNY = new Timer(DoTaskCNY, null, TimeSpan.Zero, TimeSpan.Zero);
            timerUSDT = new Timer(DoTaskUSDT, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTaskTHB(object state)
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
                    var time = dateTimeHelper.GetTimeByMinute(last, 0, "yyyy-MM-dd HH:mm:ss", timerTime);
                    bool result = await wagerIconicBLL.GetWagersAsync(tpId, time.startUnix, time.endUnix, currency);
                    if (result == true)
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
                logger.LogError(ex.Message);
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

        private async void DoTaskMYR(object state)
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
                    var time = dateTimeHelper.GetTimeByMinute(last, 0, "yyyy-MM-dd HH:mm:ss", timerTime);
                    bool result = await wagerIconicBLL.GetWagersAsync(tpId, time.startUnix, time.endUnix, currency);
                    if (result == true)
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
                logger.LogError(ex.Message);
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
                    var time = dateTimeHelper.GetTimeByMinute(last, 0, "yyyy-MM-dd HH:mm:ss", timerTime);
                    bool result = await wagerIconicBLL.GetWagersAsync(tpId, time.startUnix, time.endUnix, currency);
                    if (result == true)
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
                logger.LogError(ex.Message);
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

        private async void DoTaskVND(object state)
        {
            try
            {
                var currency = "VND";
                quickVND = 0;
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
                    var time = dateTimeHelper.GetTimeByMinute(last, 0, "yyyy-MM-dd HH:mm:ss", timerTime);
                    bool result = await wagerIconicBLL.GetWagersAsync(tpId, time.startUnix, time.endUnix, currency);
                    if (result == true)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                    }
                }
                TimeSpan ts = DateTime.Now - timerTime;
                int diffMin = (int)ts.TotalMinutes;
                if (diffMin > beyond)
                {
                    quickVND = 1;
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            finally
            {
                if (quickVND == 1)
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

        private async void DoTaskCNY(object state)
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
                    var time = dateTimeHelper.GetTimeByMinute(last, 0, "yyyy-MM-dd HH:mm:ss", timerTime);
                    bool result = await wagerIconicBLL.GetWagersAsync(tpId, time.startUnix, time.endUnix, currency);
                    if (result == true)
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
                logger.LogError(ex.Message);
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

        private async void DoTaskUSDT(object state)
        {
            try
            {
                var currency = "USDT";
                quickUSDT = 0;
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
                    var time = dateTimeHelper.GetTimeByMinute(last, 0, "yyyy-MM-dd HH:mm:ss", timerTime);
                    bool result = await wagerIconicBLL.GetWagersAsync(tpId, time.startUnix, time.endUnix, currency);
                    if (result == true)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                    }
                }
                TimeSpan ts = DateTime.Now - timerTime;
                int diffMin = (int)ts.TotalMinutes;
                if (diffMin > beyond)
                {
                    quickUSDT = 1;
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            finally
            {
                if (quickUSDT == 1)
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(45);
                    timerUSDT.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timerUSDT.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            //WriteToFile("WriteToFileHostedService: Process Stopped");
            timerTHB?.Change(Timeout.Infinite, 0);
            timerMYR?.Change(Timeout.Infinite, 0);
            timerIDR?.Change(Timeout.Infinite, 0);
            timerVND?.Change(Timeout.Infinite, 0);
            timerCNY?.Change(Timeout.Infinite, 0);
            timerUSDT?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
