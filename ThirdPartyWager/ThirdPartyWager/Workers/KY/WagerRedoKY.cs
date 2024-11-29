using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.KY;

namespace ThirdPartyWager.Workers
{
    public class WagerRedoKY : BackgroundService
    {
        private Timer timer;
        private readonly ILogger<WagerRedoKY> _logger;
        private readonly WagerKYBLL _wagerKYBLL;
        private readonly CommonBLL _commonBLL;
        private readonly int tpType = (int)WagerTimerType.RedoWager;
        private readonly int timeRange = 60; //in minutes
        private readonly int quickTime = 5;  // in minutes
        private readonly int runTime = 1;    // in hours
        private readonly string _tpName = Tp.KY.ToString();
        private readonly int tpId = (int)Tp.KY;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly Dictionary<int, KYConfig> _siteConfigDic;

        public WagerRedoKY(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerRedoKY>>();
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
            var isFast = false;
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
                            var quick = await _wagerKYBLL.ExecWagerRedoWorker(config, tpType, timeRange, item.TpCurrencyId, siteId.SiteId);
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
                var dueTimeSpan = isFast ? TimeSpan.FromMinutes(quickTime) : TimeSpan.FromHours(runTime);
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
