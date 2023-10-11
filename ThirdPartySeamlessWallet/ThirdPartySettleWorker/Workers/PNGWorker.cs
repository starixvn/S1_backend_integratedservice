using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class PNGWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.PNG;
        
        private Timer timer;

        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<PNGWorker> _logger;
        private readonly string _tpName;

        public PNGWorker(SettledBLL settledBll, 
            TpUnSettledBLL tpUnSettledBll,
            TpCustInfoBLL tpCustInfoBll, 
            ILogger<PNGWorker> logger)
        {
            _settledBLL = settledBll;
            _tpUnSettledBLL = tpUnSettledBll;
            _tpCustInfoBLL = tpCustInfoBll;
            _logger = logger;

            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoSettle, GameType.PNG_RNG, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettle(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gameType = GameType.PNG_RNG;
            var tpCategory = TpCategory.Slots;

            var getSP = "Service_BettransRng_PNG_Unsettle_Get_V3";
            var setSP = "Service_BettransRng_PNG_Settle_Set_V3";

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);

                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;
                foreach (var transId in tpUnsettleList.Select(tpUnsettle => tpUnsettle.TransId))
                {




                }


            }
            catch (Exception e)
            {
                


            }

        }
    }
}
