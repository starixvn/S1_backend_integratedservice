using Serilog.Context;

using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerDG : BackgroundService
    {
        private readonly ILogger<WagerDG> _logger;
        private readonly WagerDGBLL _wagerDGBLL;
        private readonly IHostEnvironment _environment;
        private Timer _timer;
        private readonly string _tpName = Tp.DG.ToString();

        public WagerDG(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerDG>>();
            _wagerDGBLL = provider.GetRequiredService<WagerDGBLL>();
            _environment = provider.GetRequiredService<IHostEnvironment>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        private async void DoTask(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var isContinue = false;
            try
            {
                isContinue = await _wagerDGBLL.GetWagersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(1);
                if (_environment.IsDevelopment() || _environment.IsEnvironment("QA") || isContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(10);
                }

                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

    }
}
