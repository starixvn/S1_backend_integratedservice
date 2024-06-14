using Serilog.Context;

using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Workers
{
    public class WagerIM : BackgroundService
    {
        private readonly ILogger<WagerIM> _logger;
        private readonly WagerIMBLL _wagerIMBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;

        private Timer _timer;
        private const int TpType = (int)WagerTimerType.Wager;
        private readonly string _tpName = Tp.IM.ToString();
        private const int TpId = (int)Tp.IM;
        private readonly Dictionary<int, IMConfig> _siteConfigDic;

        public WagerIM(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerIM>>();
            _wagerIMBLL = provider.GetRequiredService<WagerIMBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<IMConfig>(Tp.IM.ToString());
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
                    var _setting = _siteConfigDic[0];

                    if (_setting == null) continue;
                    sec = await _wagerIMBLL.ExecWagerWorker(_setting, siteData, TpType);
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
