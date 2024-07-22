using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Workers
{
    public class WagerRedoPlayTech : BackgroundService
    {
        private readonly ILogger<WagerRedoPlayTech> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly WagerPlayTechBLL _wagerPlayTechBLL;
        private readonly Dictionary<int, PlayTechConfig> _siteConfigDic;

        private Timer timer;
        private readonly int tpType = (int)WagerTimerType.RedoWager;
        private readonly int timeRange = 15; //in minutes 
        private readonly int quickTime = 90; // in seconds
        private readonly int runTime = 17; // in minutes
        private readonly string _tpName = Tp.PlayTech.ToString();
        private readonly int _tpId = (int)Tp.PlayTech;

        public WagerRedoPlayTech(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerRedoPlayTech>>();
            _wagerPlayTechBLL = provider.GetRequiredService<WagerPlayTechBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<PlayTechConfig>(Tp.PlayTech.ToString());
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
                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
                foreach (var siteData in siteList)
                {
                    if (_siteConfigDic.TryGetValue(siteData.SiteId, out var _setting))
                    {
                        var res = await _wagerPlayTechBLL.ExecWagerRedoWorker(siteData, tpType, timeRange, _setting);
                        if (res)
                        {
                            quick = true;
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
                var dueTimeSpan = quick ? TimeSpan.FromSeconds(quickTime) : TimeSpan.FromMinutes(runTime);
                timer?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
