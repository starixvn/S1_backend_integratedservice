using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerRedoEVO : BackgroundService
    {
        private readonly ILogger<WagerRedoEVO> _logger;
        private readonly WagerEVOBLL _wagerEVOBLL;

        private Timer timer;
        private readonly int tpType = (int)WagerTimerType.RedoWager;
        private readonly int timeRange = 60; //in minutes 
        private readonly int quickTime = 45; // in seconds
        private readonly int runTime = 1; // in hours
        private readonly string _tpName = Tp.EVO.ToString();

        public WagerRedoEVO(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerRedoEVO>>();
            _wagerEVOBLL = provider.GetRequiredService<WagerEVOBLL>();
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
                quick = await _wagerEVOBLL.ExecWagerRedoWorker(tpType, timeRange);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = quick ? TimeSpan.FromSeconds(quickTime) : TimeSpan.FromHours(runTime);
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
