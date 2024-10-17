using Serilog.Context;

using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class TpRefundWorker : BackgroundService
    {
        private readonly ILogger<TpRefundWorker> _logger;
        private readonly TpRefundBLL _tpRefundBLL;

        // timer{gameType} 
        private Timer timer35;
        private Timer timer36;
        private Timer timer37;

        public TpRefundWorker(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<TpRefundWorker>>();
            _tpRefundBLL = provider.GetRequiredService<TpRefundBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer35 = new Timer(DoTask, GameType.One789_Lottery, TimeSpan.Zero, TimeSpan.Zero);
            timer36 = new Timer(DoTask, GameType.One789_Lottery_789, TimeSpan.Zero, TimeSpan.Zero);
            timer37 = new Timer(DoTask, GameType.One789_LiveCasino, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void DoTask(object state)
        {
            var tpName = (GameType)state switch
            {
                GameType.One789_Lottery => Tp.One789.ToString(),
                GameType.One789_Lottery_789 => Tp.One789.ToString(),
                GameType.One789_LiveCasino => Tp.One789.ToString(),
                _ => string.Empty
            };
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", tpName);
            var task = true;
            var gametype = (GameType)state;
            try
            {
                task = _tpRefundBLL.ExecRefundWorker((int)gametype);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(20);
                if (!task)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(10);
                }
                switch ((GameType)state)
                {
                    case GameType.One789_Lottery:
                        dueTimeSpan = TimeSpan.FromHours(2);
                        if (!task)
                        {
                            dueTimeSpan = TimeSpan.FromHours(1);
                        }
                        timer35.Change(dueTimeSpan, TimeSpan.Zero);
                        break;
                    case GameType.One789_Lottery_789:
                        timer36.Change(dueTimeSpan, TimeSpan.Zero);
                        break;
                    case GameType.One789_LiveCasino:
                        timer37.Change(dueTimeSpan, TimeSpan.Zero);
                        break;
                    default:
                        break;
                }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer35?.Change(Timeout.Infinite, 0);
            timer36?.Change(Timeout.Infinite, 0);
            timer37?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
