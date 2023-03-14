using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers;

public class WagerBG : BackgroundService
{
    private readonly ILogger<WagerBG> _logger;
    private readonly WagerBGBLL _wagerBGBLL;
    private readonly CommonBLL _commonBLL;

    private readonly int tpId = (int)Tp.BG;
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
            var TpCurrencyList = _commonBLL.GetTpCurrencyList((int)Tp.BG);
            foreach (var siteInfo in siteList)
            {
                //var currencyList = JsonSerializer.Deserialize<IEnumerable<int>>(siteInfo.CurrencyList);
                //foreach (var item in TpCurrencyList.Where(x => currencyList.Contains(x.CurrencyId)))
                //{
                //    var isSuccess = true;
                //    var isContinue = true;
                //    while (isSuccess && isContinue)
                //    {
                //        (isSuccess, isContinue) = await _wagerYeebetBLL.GetWagersAsync(tpId, siteInfo.SiteId, item.TpCurrencyId);
                //        Thread.Sleep(3 * 1000);
                //    }
                //}
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        finally
        {
            var dueTimeSpan = TimeSpan.FromMinutes(1);

            _timer.Change(dueTimeSpan, TimeSpan.Zero);
        }
    }
}