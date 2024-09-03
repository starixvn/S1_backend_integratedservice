using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerUnsettleCheckSkywind : BackgroundService
    {
        private readonly ILogger<WagerUnsettleCheckSkywind> _logger;
        private readonly WagerSkywindBLL _wagerSkywindBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private Timer _timer;
        private readonly string _tpName = Tp.Skywind.ToString();
        private readonly int _tpId = (int)Tp.Skywind;

        public WagerUnsettleCheckSkywind(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerUnsettleCheckSkywind>>();
            _wagerSkywindBLL = provider.GetRequiredService<WagerSkywindBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
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
            var intervalMinutes = 5;
            var nextTimeSpan = TimeSpan.FromMinutes(intervalMinutes);
            try
            {
                var (isSuccess, doQuick) = await _wagerSkywindBLL.UnsettleCheck();

                if (doQuick)
                {
                    nextTimeSpan = TimeSpan.FromMinutes(1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Skywind.SettleCheck.Exception");
                nextTimeSpan = TimeSpan.FromMinutes(1);
            }
            finally
            {
                _timer.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
