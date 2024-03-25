using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerRedoGPI : BackgroundService
    {
        private Timer timerLT;
        private Timer timerRNG;
        private readonly ILogger<WagerRedoGPI> _logger;
        private readonly WagerGPIBLL _wagerGPIBLL;
        private readonly int tpType = (int)WagerTimerType.RedoWager;
        private readonly int timeRange = 60; //in minutes 
        private readonly int quickTime = 307; // in seconds
        private readonly int runTime = 1; // in hours
        private readonly string _tpName = Tp.GPI.ToString();

        public WagerRedoGPI(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerRedoGPI>>();
            _wagerGPIBLL = provider.GetRequiredService<WagerGPIBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // lottery 統一使用一款取得注單即可
            timerLT = new Timer(DoTask, GameType.GPI_Keno, TimeSpan.FromSeconds(46), TimeSpan.Zero);
            timerRNG = new Timer(DoTask, GameType.GPI_RNG, TimeSpan.FromSeconds(71), TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTask(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var quick = false;
            var gameType = (GameType)state;
            try
            {
                quick = await _wagerGPIBLL.ExecWagerRedoWorker(tpType, (int)gameType, timeRange);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = quick ? TimeSpan.FromSeconds(quickTime) : TimeSpan.FromHours(runTime);
                _ = (gameType == GameType.GPI_Keno) ? timerLT?.Change(dueTimeSpan, TimeSpan.Zero) : timerRNG?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerLT?.Change(Timeout.Infinite, 0);
            timerRNG?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
