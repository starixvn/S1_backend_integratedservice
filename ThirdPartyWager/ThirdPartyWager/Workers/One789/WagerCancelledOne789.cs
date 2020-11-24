using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerCancelledOne789 : BackgroundService
    {
        private readonly ILogger<WagerCancelledOne789> logger;
        private readonly WagerCancelledOne789BLL wagerCancelledOne789BLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        //private readonly DateTimeHelper dateTimeHelper;
        private readonly IHostEnvironment environment;
        private Timer timer;
        private int tpId = (int)Tp.One789;
        private int tpType = 3; // cancelled

        public WagerCancelledOne789(
            ILogger<WagerCancelledOne789> logger,
            WagerCancelledOne789BLL wagerCancelledOne789BLL,
            ThirdPartyBLL thirdPartyBLL,
            //DateTimeHelper dateTimeHelper,
            IHostEnvironment environment
        )
        {
            this.logger = logger;
            this.wagerCancelledOne789BLL = wagerCancelledOne789BLL;
            this.thirdPartyBLL = thirdPartyBLL;
            //this.dateTimeHelper = dateTimeHelper;
            this.environment = environment;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTask(object state)
        {
            var gameType = (int)GameType.One789_LiveCasino;
            var isContinue = false;
            //try
            //{
            //    var dataTpTimer = thirdPartyBLL.GetThirdPartyTimerReferenceByGameType(tpId, tpType, gameType);
            //    string date = null;
            //    string lastId = null;
            //    if (dataTpTimer == null)
            //    {
            //        date = DateTime.Now.ToString("yyyy-MM-dd");
            //    }
            //    else
            //    {
            //        lastId = dataTpTimer.Reference;
            //    }

            //    var resp = await wagerCancelledOne789BLL.GetWagersCancelledAsync(date, lastId, gameType);
            //    isContinue = resp.result;
            //    lastId = resp.lastId;
            //    if (isContinue == true)
            //    {
            //        thirdPartyBLL.SetThirdPartyTimerByGameType(tpId, tpType, gameType, lastId);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    logger.LogError(ex.Message);
            //}
            //finally
            //{
            //    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(45);
            //    if (environment.IsDevelopment() || environment.IsEnvironment("QA") || isContinue)
            //    {
            //        dueTimeSpan = TimeSpan.FromSeconds(10);
            //    }

            //    timer.Change(dueTimeSpan, TimeSpan.Zero);
            //}
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            //WriteToFile("WriteToFileHostedService: Process Stopped");
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
