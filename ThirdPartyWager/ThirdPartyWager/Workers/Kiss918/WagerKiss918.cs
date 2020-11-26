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
    public class WagerKiss918 : BackgroundService
    {
        private readonly ILogger<WagerKiss918> logger;
        private readonly WagerKiss918BLL wagerKiss918BLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly DateTimeHelper dateTimeHelper;
        private readonly IHostEnvironment environment;
        private Timer timerTHB;
        private Timer timerMYR;
        private Timer timerIDR;
        private int intervalMins = 3;
        private int iRangMins = 20;
        private int tpId = (int)Tp.Kiss918;
        private int tpType = 1;
        private int quickTHB = 0;
        private int quickMYR = 0;
        private int quickIDR = 0;

        public WagerKiss918(
            ILogger<WagerKiss918> logger,
            WagerKiss918BLL wagerKiss918BLL,
            ThirdPartyBLL thirdPartyBLL,
            DateTimeHelper dateTimeHelper,
            IHostEnvironment environment
        )
        {
            this.logger = logger;
            this.wagerKiss918BLL = wagerKiss918BLL;
            this.thirdPartyBLL = thirdPartyBLL;
            this.dateTimeHelper = dateTimeHelper;
            this.environment = environment;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerTHB = new Timer(DoTaskTHB, null, TimeSpan.Zero, TimeSpan.Zero);
            timerMYR = new Timer(DoTaskMYR, null, TimeSpan.Zero, TimeSpan.Zero);
            timerIDR = new Timer(DoTaskIDR, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTaskTHB(object state)
        {
            try
            {
                var currency = "THB";
                quickTHB = 0;
                var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerByReference(tpId, tpType, currency);
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
                    var time = dateTimeHelper.GetTimeByMinute(last, 8, "yyyy-MM-dd HH:mm:ss", timerTime);
                    bool result = await wagerKiss918BLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency);
                    if (result == true)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByReference(tpId, tpType, time.endUnix, currency);
                    }
                }
                TimeSpan ts = DateTime.Now - timerTime;
                int diffMin = (int)ts.TotalMinutes;
                if (diffMin > 5)
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
                var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerByReference(tpId, tpType, currency);
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
                    var time = dateTimeHelper.GetTimeByMinute(last, 8, "yyyy-MM-dd HH:mm:ss", timerTime);
                    bool result = await wagerKiss918BLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency);
                    if (result == true)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByReference(tpId, tpType, time.endUnix, currency);
                    }
                }
                TimeSpan ts = DateTime.Now - timerTime;
                int diffMin = (int)ts.TotalMinutes;
                if (diffMin > 5)
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
                var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerByReference(tpId, tpType, currency);
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
                    var time = dateTimeHelper.GetTimeByMinute(last, 8, "yyyy-MM-dd HH:mm:ss", timerTime);
                    bool result = await wagerKiss918BLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency);
                    if (result == true)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByReference(tpId, tpType, time.endUnix, currency);
                    }
                }
                TimeSpan ts = DateTime.Now - timerTime;
                int diffMin = (int)ts.TotalMinutes;
                if (diffMin > 5)
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

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            //WriteToFile("WriteToFileHostedService: Process Stopped");
            timerTHB?.Change(Timeout.Infinite, 0);
            timerMYR?.Change(Timeout.Infinite, 0);
            timerIDR?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
