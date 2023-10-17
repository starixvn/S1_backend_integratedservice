using Serilog.Context;

using ThirdPartyWager.Service.BLL;

using static ThirdPartyWager.Extensions.MethodExtension;

namespace ThirdPartyWager.Workers
{
    public class SabaOdds : BackgroundService
    {
        private readonly IHostEnvironment _environment;
        private readonly ILogger<SabaOdds> _logger;
        private readonly SabaOddsBLL _sabaOddsBll;
        private Timer _timerSabaOdds;
        private readonly string _tpName = "SabaOdds";

        public SabaOdds(IServiceProvider provider)
        {
            _environment = provider.GetRequiredService<IHostEnvironment>();
            _logger = provider.GetRequiredService<ILogger<SabaOdds>>();
            _sabaOddsBll = provider.GetRequiredService<SabaOddsBLL>();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerSabaOdds = new Timer(DoSabaOddsAsync, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        private async void DoSabaOddsAsync(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            LogContext.PushProperty("JobName", nameof(DoSabaOddsAsync));

            var isError = false;
            try
            {
                isError = await _sabaOddsBll.UpdateHotEventOddsWithHdp();
            }
            catch (Exception ex)
            {
                isError = true;
                _logger.LogError(ex, GetCurrentAsyncMethod());
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(1);
                if (_environment.IsDevelopment())
                {
                    dueTimeSpan = TimeSpan.FromSeconds(30);
                }

                if (isError)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }

                _timerSabaOdds.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerSabaOdds?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
