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
    public class WagerMega : BackgroundService
    {
        private readonly ILogger<WagerMega> logger;
        private readonly WagerMegaBLL wagerMegaBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly DateTimeHelper dateTimeHelper;
        private Timer timerTHB;
        private Timer timerMYR;
        private Timer timerIDR;
        private int intervalDays = 1;
        private int iRangMins = 1440;
        private int tpId = (int)Tp.Mega;
        private readonly string _tpName = Tp.Mega.ToString();
        private int tpType = 1;
        private int quickTHB = 0;
        private int quickMYR = 0;
        private int quickIDR = 0;
        private int iDays = 1;

        public WagerMega(
            ILogger<WagerMega> logger,
            WagerMegaBLL wagerMegaBLL,
            ThirdPartyBLL thirdPartyBLL,
            DateTimeHelper dateTimeHelper,
            IHostEnvironment environment
        )
        {
            this.logger = logger;
            this.wagerMegaBLL = wagerMegaBLL;
            this.thirdPartyBLL = thirdPartyBLL;
            this.dateTimeHelper = dateTimeHelper;
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
                if (dataTpTimer == null)
                {
                    timerTime = DateTime.Now.AddDays(iDays * -1);
                }
                else
                {
                    timerTime = dataTpTimer.StartTime.AddSeconds(1);
                }

                if (timerTime < DateTime.Now.AddDays(iDays * -1))
                {
                    var time = dateTimeHelper.GetTimeByDay(iRangMins, -4, "yyyy-MM-dd HH:mm:ss", timerTime);
                    var result = await wagerMegaBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency);
                    if (result == true)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                    }
                }
                var diffDay = (int)(DateTime.Now.Date - timerTime.Date).TotalDays;
                if (diffDay >= (iDays + 1))
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
                    var dueTimeSpan = TimeSpan.FromSeconds(60);
                    timerTHB.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromDays(intervalDays);
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
                if (dataTpTimer == null)
                {
                    timerTime = DateTime.Now.AddDays(iDays * -1);
                }
                else
                {
                    timerTime = dataTpTimer.StartTime.AddSeconds(1);
                }

                if (timerTime < DateTime.Now.AddDays(iDays * -1))
                {
                    var time = dateTimeHelper.GetTimeByDay(iRangMins, -4, "yyyy-MM-dd HH:mm:ss", timerTime);
                    var result = await wagerMegaBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency);
                    if (result == true)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                    }
                }
                var diffDay = (int)(DateTime.Now.Date - timerTime.Date).TotalDays;
                if (diffDay >= (iDays + 1))
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
                    var dueTimeSpan = TimeSpan.FromSeconds(60);
                    timerMYR.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromDays(intervalDays);
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
                if (dataTpTimer == null)
                {
                    timerTime = DateTime.Now.AddDays(iDays * -1);
                }
                else
                {
                    timerTime = dataTpTimer.StartTime.AddSeconds(1);
                }

                if (timerTime < DateTime.Now.AddDays(iDays * -1))
                {
                    var time = dateTimeHelper.GetTimeByDay(iRangMins, -4, "yyyy-MM-dd HH:mm:ss", timerTime);
                    var result = await wagerMegaBLL.GetWagersAsync(tpId, time.startTime, time.endTime, currency);
                    if (result == true)
                    {
                        thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, currency);
                    }
                }
                var diffDay = (int)(DateTime.Now.Date - timerTime.Date).TotalDays;
                if (diffDay >= (iDays + 1))
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
                    var dueTimeSpan = TimeSpan.FromSeconds(60);
                    timerMYR.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromDays(intervalDays);
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
