using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerMG : BackgroundService
    {
        private readonly ILogger<WagerMG> _logger;
        private readonly IMGBLL _mgBll;
        private Timer _timer;
        private readonly string _tpName = Tp.MG.ToString();

        public WagerMG(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerMG>>();
            _mgBll = provider.GetRequiredService<IMGBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTask(object? state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            try
            {
                await _mgBll.ReSettle();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WagerMG.DoTask.Exception");
            }
            finally
            {
                _timer.Change(TimeSpan.FromMinutes(10), TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}