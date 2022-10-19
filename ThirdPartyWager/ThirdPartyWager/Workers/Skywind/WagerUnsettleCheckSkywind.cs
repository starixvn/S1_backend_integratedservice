using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerUnsettleCheckSkywind : BackgroundService
    {
        private readonly ILogger<WagerUnsettleCheckSkywind> _logger;
        private readonly WagerSkywindBLL _wagerSkywindBLL;
        private Timer _timerNova88;
        private Timer _timerGoal123;
        private Timer _timerBwing;
        private readonly string _tpName = Tp.Skywind.ToString();
        public WagerUnsettleCheckSkywind(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerUnsettleCheckSkywind>>();
            _wagerSkywindBLL = provider.GetRequiredService<WagerSkywindBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerNova88 = new Timer(DoNova88UnsettleCheck, 1, TimeSpan.Zero, TimeSpan.Zero);
            _timerGoal123 = new Timer(DoGoal123UnsettleCheck, 2, TimeSpan.Zero, TimeSpan.Zero);
            _timerBwing = new Timer(DoBwingUnsettleCheck, 3, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoNova88UnsettleCheck(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var intervalMinutes = 5;
            var nextTimeSpan = TimeSpan.FromMinutes(intervalMinutes);
            var siteId = (int)state;
            try
            {
                var (isSuccess, doQuick) = await _wagerSkywindBLL.UnsettleCheck(siteId);

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
                _timerNova88.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }
        private async void DoBwingUnsettleCheck(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var intervalMinutes = 5;
            var nextTimeSpan = TimeSpan.FromMinutes(intervalMinutes);
            var siteId = (int)state;
            try
            {
                var (isSuccess, doQuick) = await _wagerSkywindBLL.UnsettleCheck(siteId);

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
                _timerBwing.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }
        private async void DoGoal123UnsettleCheck(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var intervalMinutes = 5;
            var nextTimeSpan = TimeSpan.FromMinutes(intervalMinutes);
            var siteId = (int)state;
            try
            {
                var (isSuccess, doQuick) = await _wagerSkywindBLL.UnsettleCheck(siteId);

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
                _timerGoal123.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerNova88?.Change(Timeout.Infinite, 0);
            _timerBwing?.Change(Timeout.Infinite, 0);
            _timerGoal123?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
