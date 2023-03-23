using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Interfaces;
using static ThirdPartySeamlessWallet.Controllers.APController;

namespace ThirdPartySettleWorker.Workers
{
    internal class TFGWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp { get => Tp.TFG; }
        private Timer timer;
        private readonly string _tpName;
        public TFGWorker(IServiceProvider provider)
        {
            _tpName = Enum.GetName(typeof(Tp), Tp);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(SettleEsports, GameType.TFG_Esports, TimeSpan.Zero, TimeSpan.Zero);

            return Task.CompletedTask;
        }
        private void SettleEsports(object state)
        {
            //LogContext.PushProperty("TP", _tpName);
            //LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            //try
            //{
            //}
            //catch
            //{

            //}
        }
    }
}
