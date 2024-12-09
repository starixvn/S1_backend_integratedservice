using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.KY;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerKY : BackgroundService
    {
        private readonly ILogger<WagerKY> _logger;
        private readonly WagerKYBLL _wagerKYBLL;
        private readonly CommonBLL _commonBLL;
        private Timer timer;
        private readonly int tpType = (int)WagerTimerType.Wager;
        private readonly int timeRange = 20; // in minutes
        private readonly int quickTime = 45; // in seconds
        private readonly int runTime = 120;  // in seconds
        private readonly string _tpName = Tp.KY.ToString();
        private readonly int tpId = (int)Tp.KY;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly Dictionary<int, KYConfig> _siteConfigDic;

        public WagerKY(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerKY>>();
            _wagerKYBLL = provider.GetRequiredService<WagerKYBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<KYConfig>(Tp.KY.ToString());
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
            
            var quick = false;
            try
            {
                var siteList = _thirdPartyBLL.GetTpListByTpId(tpId).Where(x => x.TicketKey == 1);
                var TpCurrencyList = _commonBLL.GetTpCurrencyList(tpId);
                foreach (var siteId in siteList)
                {
                    if (_siteConfigDic.TryGetValue(siteId.SiteId, out var config))
                    {
                        foreach (var item in TpCurrencyList.Where(item => config.CurrencyData.ContainsKey(item.TpCurrencyId)))
                        {
                            quick = await _wagerKYBLL.ExecWagerWorker(config, tpType, timeRange, item.TpCurrencyId, siteId.SiteId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = quick ? TimeSpan.FromSeconds(quickTime) : TimeSpan.FromSeconds(runTime);
                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }

        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}