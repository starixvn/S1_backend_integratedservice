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
    public class WagerRedoKiss918 : BackgroundService
    {
        private readonly ILogger<WagerRedoKiss918> logger;
        private readonly WagerRedoKiss918BLL wagerRedoKiss918BLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly DateTimeHelper dateTimeHelper;
        private readonly IHostEnvironment environment;
        private Timer timerTHB;
        private Timer timerMYR;
        private Timer timerIDR;
        private int intervalMins = 60;
        private int tpId = (int)Tp.Kiss918;
        private readonly string _tpName = Tp.Kiss918.ToString();
        private int tpType = 2;
        private int quickTHB = 0;
        private int quickMYR = 0;
        private int quickIDR = 0;

        public WagerRedoKiss918(
            ILogger<WagerRedoKiss918> logger,
            WagerRedoKiss918BLL wagerRedoKiss918BLL,
            ThirdPartyBLL thirdPartyBLL,
            DateTimeHelper dateTimeHelper,
            IHostEnvironment environment
        )
        {
            this.logger = logger;
            this.wagerRedoKiss918BLL = wagerRedoKiss918BLL;
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
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            try
            {
                var currency = "THB";
                quickTHB = 0;
                var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);
                DateTime timerTime;
                int last;
                if (dataTpTimer == null)
                {
                    timerTime = DateTime.Now.AddMinutes(-2 * intervalMins);
                    last = intervalMins;
                }
                else
                {
                    timerTime = dataTpTimer.StartTime.AddSeconds(1);
                    last = intervalMins;
                }

                if (timerTime < DateTime.Now)
                {
                    var time = dateTimeHelper.GetTimeByHour(last, 8, "yyyy-MM-dd HH:mm:ss", timerTime);
                    bool result = await wagerRedoKiss918BLL.RedoWagersAsync(tpId, time.startTime, time.endTime, time.startUnix, time.endUnix, currency);
                    if (result == true)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                    }
                }
                TimeSpan ts = DateTime.Now - timerTime;
                int diffMin = (int)ts.TotalMinutes;
                if (diffMin >= 120)
                {
                    quickTHB = 1;
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
            finally
            {
                if (quickTHB == 1)
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(120);
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
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            try
            {
                var currency = "MYR";
                quickMYR = 0;
                var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);
                DateTime timerTime;
                int last;
                if (dataTpTimer == null)
                {
                    timerTime = DateTime.Now.AddMinutes(-2 * intervalMins);
                    last = intervalMins;
                }
                else
                {
                    timerTime = dataTpTimer.StartTime.AddSeconds(1);
                    last = intervalMins;
                }

                if (timerTime < DateTime.Now)
                {
                    var time = dateTimeHelper.GetTimeByHour(last, 8, "yyyy-MM-dd HH:mm:ss", timerTime);
                    bool result = await wagerRedoKiss918BLL.RedoWagersAsync(tpId, time.startTime, time.endTime, time.startUnix, time.endUnix, currency);
                    if (result == true)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                    }
                }
                TimeSpan ts = DateTime.Now - timerTime;
                int diffMin = (int)ts.TotalMinutes;
                if (diffMin >= 120)
                {
                    quickMYR = 1;
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
            finally
            {
                if (quickMYR == 1)
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(120);
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
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            try
            {
                var currency = "IDR";
                quickIDR = 0;
                var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, currency);
                DateTime timerTime;
                int last;
                if (dataTpTimer == null)
                {
                    timerTime = DateTime.Now.AddMinutes(-2 * intervalMins);
                    last = intervalMins;
                }
                else
                {
                    timerTime = dataTpTimer.StartTime.AddSeconds(1);
                    last = intervalMins;
                }

                if (timerTime < DateTime.Now)
                {
                    var time = dateTimeHelper.GetTimeByHour(last, 8, "yyyy-MM-dd HH:mm:ss", timerTime);
                    bool result = await wagerRedoKiss918BLL.RedoWagersAsync(tpId, time.startTime, time.endTime, time.startUnix, time.endUnix, currency);
                    if (result == true)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                    }
                }
                TimeSpan ts = DateTime.Now - timerTime;
                int diffMin = (int)ts.TotalMinutes;
                if (diffMin >= 120)
                {
                    quickIDR = 1;
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
            finally
            {
                if (quickIDR == 1)
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(120);
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
