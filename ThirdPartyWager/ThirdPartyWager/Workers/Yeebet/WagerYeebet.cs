using System.Text.Json;
using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers;

public class WagerYeebet : BackgroundService
{
    private readonly ILogger<WagerYeebet> _logger;
    private readonly WagerYeebetBLL _wagerYeebetBLL;
    private readonly CommonBLL _commonBLL;
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly int tpId = (int)Tp.Yeebet;
    private Timer _timer;

    public WagerYeebet(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<WagerYeebet>>();
        _wagerYeebetBLL = provider.GetRequiredService<WagerYeebetBLL>();
        _commonBLL = provider.GetRequiredService<CommonBLL>();
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
        return Task.CompletedTask;
    }

    private async void DoTask(object state)
    {
        LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
        LogContext.PushProperty("Tp", Tp.Yeebet.ToString());
        try
        {
            var siteList = _thirdPartyBLL.GetTpListByTpId(tpId);
            var serverList = _commonBLL.GetSiteList();
            var TpCurrencyList = _commonBLL.GetTpCurrencyList(tpId);
            foreach (var siteInfo in siteList)
            {
                var currencyList = JsonSerializer.Deserialize<IEnumerable<int>>(serverList.First(x => x.SiteId == siteInfo.SiteId).CurrencyList);
                foreach (var item in TpCurrencyList.Where(x => currencyList.Contains(x.CurrencyId)))
                {
                    var isSuccess = true;
                    var isContinue = true;
                    while (isSuccess && isContinue)
                    {
                        (isSuccess, isContinue) = await _wagerYeebetBLL.GetWagersAsync(tpId, siteInfo.SiteId, item.TpCurrencyId);
                        Thread.Sleep(3 * 1000);
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
            var dueTimeSpan = TimeSpan.FromMinutes(1);

            _timer.Change(dueTimeSpan, TimeSpan.Zero);
        }
    }
}
