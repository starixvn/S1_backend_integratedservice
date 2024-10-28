using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Workers;

public class WagerBG : BackgroundService
{
    private readonly ILogger<WagerBG> _logger;
    private readonly WagerBGBLL _wagerBGBLL;
    private readonly CommonBLL _commonBLL;
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly Dictionary<int, BGConfig> _siteConfigDic;

    private readonly int tpId = (int)Tp.BG;
    private readonly WagerTimerType tpType = WagerTimerType.Wager;
    private bool isFast = false;
    private readonly int timeRange = 30; //in minutes 
    private readonly int quickTime = 15; // in seconds
    private readonly int runTime = 60; // in seconds
    private readonly int needQuickTime = 20; // in minutes
    private Timer _timer;


    public WagerBG(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<WagerBG>>();
        _wagerBGBLL = provider.GetRequiredService<WagerBGBLL>();
        _commonBLL = provider.GetRequiredService<CommonBLL>();
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<BGConfig>(Tp.BG.ToString());
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
        return Task.CompletedTask;
    }

    private async void DoTask(object state)
    {
        LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
        LogContext.PushProperty("Tp", Tp.BG.ToString());
        try
        {
            isFast = false;
            var siteList = _thirdPartyBLL.GetTpListByTpId(tpId);
            var TpCurrencyList = _commonBLL.GetTpCurrencyList(tpId);
            var quick = false;
            foreach (var siteInfo in siteList)
            {
                var _setting = _siteConfigDic[siteInfo.SiteId];
                if (_setting == null) continue;

                foreach (var item in TpCurrencyList)
                {
                    if (!_setting.CurrencyData.ContainsKey(item.TpCurrencyId))
                    {
                        continue;
                    }
                    var currencyData = _setting.CurrencyData[item.TpCurrencyId];

                    quick = await _wagerBGBLL.ExecWagerWorker(siteInfo.SiteId, item.TpCurrencyId, tpType, timeRange, needQuickTime);
                    if (!isFast && quick) isFast = true;
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
            _timer.Change(dueTimeSpan, TimeSpan.Zero);
        }
    }
}