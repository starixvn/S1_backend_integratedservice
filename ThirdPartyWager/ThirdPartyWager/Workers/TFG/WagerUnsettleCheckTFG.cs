using Serilog.Context;

using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Workers
{
    public class WagerUnsettleCheckTFG : BackgroundService
    {
        private readonly ILogger<WagerUnsettleCheckTFG> _logger;
        private readonly WagerTFGBLL _wagerTFGBLL;
        private Timer _timer;
        private readonly string _tpName = Tp.TFG.ToString();
        private readonly Dictionary<int, TFGConfig> _siteConfigDic;
        public WagerUnsettleCheckTFG(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerUnsettleCheckTFG>>();
            _wagerTFGBLL = provider.GetRequiredService<WagerTFGBLL>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<TFGConfig>(Tp.TFG.ToString());
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoUnsettleCheck, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        private async void DoUnsettleCheck(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var intervalMinutes = 5;
            var nextTimeSpan = TimeSpan.FromMinutes(intervalMinutes);
            try
            {
                var config = _siteConfigDic[0];
                var (isSuccess, doQuick) = await _wagerTFGBLL.UnsettleCheck(config);

                if (doQuick)
                {
                    nextTimeSpan = TimeSpan.FromMinutes(1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TFG.SettleCheck.Exception");
                nextTimeSpan = TimeSpan.FromMinutes(1);
            }
            finally
            {
                _timer.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
