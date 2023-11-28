using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerRedoNLC : BackgroundService
    {
        private readonly ILogger<WagerRedoNLC> _logger;
        private readonly WagerNLCBLL _wagerNLCBLL;

        private Timer timer;
        private readonly int tpType = (int)WagerTimerType.RedoWager;
        private readonly int timeRange = 60; //in minutes 
        private readonly int quickTime = 45; // in seconds
        private readonly int runTime = 35; // in minutes
        private readonly string _tpName = Tp.NLC.ToString();

        public WagerRedoNLC(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerRedoNLC>>();
            _wagerNLCBLL = provider.GetRequiredService<WagerNLCBLL>();
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
            var quick = false;
            try
            {
                quick = await _wagerNLCBLL.ExecWagerRedoWorker(tpType, timeRange);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = quick ? TimeSpan.FromSeconds(quickTime) : TimeSpan.FromMinutes(runTime);
                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
