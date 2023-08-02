using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerUnsettleEVO : BackgroundService
    {
        private readonly ILogger<WagerUnsettleEVO> _logger;
        private readonly WagerEVOBLL _wagerEVOBLL;

        private Timer timer;
        private readonly int tpType = (int)WagerTimerType.UnSettle;
        private readonly int runTime = 5; // in minute
        private readonly string _tpName = Tp.EVO.ToString();

        public WagerUnsettleEVO(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerUnsettleEVO>>();
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
            try
            {
                _ = await _wagerEVOBLL.ExecUnsettleWorker(tpType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                timer.Change(TimeSpan.FromMinutes(runTime), TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
