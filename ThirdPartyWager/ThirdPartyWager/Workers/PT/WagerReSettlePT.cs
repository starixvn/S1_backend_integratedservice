using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerReSettlePT : BackgroundService
    {
        private readonly ILogger<WagerReSettlePT> logger;
        private readonly WagerPTBLL wagerPTBLL;
        private readonly DateTimeHelper dateTimeHelper;
        private readonly IHostEnvironment environment;
        private Timer timer;
        private int tpId = (int)Tp.PT;
        private readonly string _tpName = Tp.PT.ToString();
        private readonly List<PTRequestModel> _list;

        public WagerReSettlePT(ILogger<WagerReSettlePT> logger, WagerPTBLL wagerPTBLL, DateTimeHelper dateTimeHelper, IHostEnvironment environment)
        {
            this.logger = logger;
            this.wagerPTBLL = wagerPTBLL;
            this.dateTimeHelper = dateTimeHelper;
            this.environment = environment;
            _list = InitWagerList();
        }
        
        private List<PTRequestModel> InitWagerList()
        {
            var list = new List<PTRequestModel>
            {
                new PTRequestModel() {SiteId = 1, Currency = "MYR", TimeZone = 8},
                new PTRequestModel() {SiteId = 1, Currency = "THB", TimeZone = 7},
                new PTRequestModel(){SiteId = 1 , Currency = "IDR" , TimeZone = 8},
                new PTRequestModel(){SiteId = 1 , Currency = "USD" , TimeZone = 0},
                new PTRequestModel(){SiteId = 1 , Currency = "CNY" , TimeZone = 8},
                new PTRequestModel(){SiteId = 1 , Currency = "INR" , TimeZone = 5},
                new PTRequestModel(){SiteId = 2 , Currency = "VND" , TimeZone = 7},
                new PTRequestModel(){SiteId = 3 , Currency = "VND" , TimeZone = 7}
            };
            return list;
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        
        private async void DoTask(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            // if (!environment.IsProduction())
            // {
            //     return;
            // }
            try
            {
                foreach (var task in _list)
                {
                    var timerTime = new DateTime(2022,04,01);
                    for (var day = 0; day < 31; day++)
                    {
                        for (var hour = 0; hour < 24; hour++)
                        {
                            for (var min = 0; min < 2; min++)
                            {
                                try
                                {
                                    var time = dateTimeHelper.GetTimeByMinute(29, task.TimeZone, "yyyy-MM-dd HH:mm:ss", timerTime);
                                    await wagerPTBLL.GetWagersAsync(tpId, time.startTime, time.endTime, task.Currency, task.SiteId, task.TimeZone , WagerType.WagerReSettle);
                                    timerTime = timerTime.AddMinutes(30);
                                }
                                catch (Exception e)
                                {
                                    logger.LogError(e, e.Message);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
            finally
            {
                // var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                // timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
        
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}