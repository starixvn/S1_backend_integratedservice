using Serilog.Context;
using System.Text.Json;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers;

public class WagerBG : BackgroundService
{
    private readonly ILogger<WagerBG> _logger;
    private readonly WagerBGBLL _wagerBGBLL;
    private readonly CommonBLL _commonBLL;

    private readonly int tpId = (int)Tp.BG;
    private readonly WagerTimerType tpType = WagerTimerType.Wager;
    private bool isFast = false;
    private readonly int timeRange = 30; //in minutes 
    private readonly int quickTime = 15; // in seconds
    private readonly int runTime = 60; // in seconds
    private readonly int needQuickTime = 20; // in minutes
    private Timer _timer;

    public WagerBG(ILogger<WagerBG> logger,
                       WagerBGBLL wagerBGBLL,
                       CommonBLL commonBll)
    {
        _logger = logger;
        _wagerBGBLL = wagerBGBLL;
        _commonBLL = commonBll;
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
            var siteList = _commonBLL.GetSiteList().Where(x => x.SiteId != 4);
            var TpCurrencyList = _commonBLL.GetTpCurrencyList(tpId);
            var quick = false;
            foreach (var siteInfo in siteList)
            {
                var currencyList = JsonSerializer.Deserialize<IEnumerable<int>>(siteInfo.CurrencyList);
                foreach (var item in TpCurrencyList.Where(x => currencyList.Contains(x.CurrencyId)))
                {
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