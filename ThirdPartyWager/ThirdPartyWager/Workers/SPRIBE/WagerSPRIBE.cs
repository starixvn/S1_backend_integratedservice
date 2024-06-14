using Serilog.Context;

using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Workers
{
    public class WagerSPRIBE : BackgroundService
    {
        private readonly ILogger<WagerSPRIBE> _logger;
        private readonly WagerSPRIBEBLL _wagerSPRIBEBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;

        private Timer _timer;
        private const int TpType = (int)WagerTimerType.Wager;
        private readonly string _tpName = Tp.SPRIBE.ToString();
        private const int TpId = (int)Tp.SPRIBE;
        private readonly Dictionary<int, SPRIBEConfig> _siteConfigDic;

        public WagerSPRIBE(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerSPRIBE>>();
            _wagerSPRIBEBLL = provider.GetRequiredService<WagerSPRIBEBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<SPRIBEConfig>(Tp.SPRIBE.ToString());
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
            var sec = 120;
            try
            {
                var siteList = _thirdPartyBLL.GetTpListByTpId(TpId);
                foreach (var siteData in siteList)
                {
                    var _setting = _siteConfigDic[siteData.SiteId];

                    if (_setting == null) continue;
                    sec = await _wagerSPRIBEBLL.ExecWagerWorker(_setting, siteData, TpType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromSeconds(sec);
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
