using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.BLL.IM;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers.IM
{
    public class WagerIM : BackgroundService
    {
        private readonly ILogger<WagerPlayTech> _logger;
        private readonly WagerIMBLL _wagerIMBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;

        private Timer _timer;
        private const int TpType = (int)WagerTimerType.Wager;
        private const int TimeRange = 10; // minutes (IM GetBetLog 時間區間最大只能10分鐘)
        private const int QuickTime = 45; // seconds
        private const int RunTime = 90; // seconds
        private readonly string _tpName = Tp.IM.ToString();
        private const int TpId = (int)Tp.IM;

        public WagerIM(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerPlayTech>>();
            _wagerIMBLL = provider.GetRequiredService<WagerIMBLL>();
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
            var quick = false;
            try
            {

                var siteList = _thirdPartyBLL.GetTpListByTpId(TpId);
                foreach (var siteData in siteList)
                {
                    quick = await _wagerIMBLL.ExecWagerWorker(siteData, TpType, TimeRange);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = quick ? TimeSpan.FromSeconds(QuickTime) : TimeSpan.FromSeconds(RunTime);
                _timer?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
    