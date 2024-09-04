using Serilog.Context;

using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;
using System.Linq;

namespace ThirdPartyWager.Workers
{
    public class WagerV8 : BackgroundService
    {
        private readonly ILogger<WagerV8> _logger;
        private readonly WagerV8BLL _wagerV8BLL;
        private readonly CommonBLL _commonBLL;
        private Timer timer;
        private readonly int tpType = (int)WagerTimerType.Wager;
        private readonly int timeRange = 20; //in minutes
        private readonly int quickTime = 45; // in seconds
        private readonly int runTime = 120;  // in seconds
        private readonly string _tpName = Tp.V8.ToString();
        private readonly int tpId = (int)Tp.V8;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly Dictionary<int, V8Config> _siteConfigDic;

        public WagerV8(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerV8>>();
            _wagerV8BLL = provider.GetRequiredService<WagerV8BLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<V8Config>(Tp.V8.ToString());
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
            var isFast = false;
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
                            quick = await _wagerV8BLL.ExecWagerWorker(config, tpType, timeRange, item.TpCurrencyId, siteId.SiteId);
                            if (!isFast && quick) isFast = true;
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
                var dueTimeSpan = isFast ? TimeSpan.FromSeconds(quickTime) : TimeSpan.FromSeconds(runTime);
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