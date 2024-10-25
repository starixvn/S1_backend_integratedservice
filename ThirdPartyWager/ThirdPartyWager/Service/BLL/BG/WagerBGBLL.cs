using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL;

public class WagerBGBLL
{
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly WagerBGService _wagerBGService;
    private readonly ILogger<WagerBGBLL> _logger;
    private readonly CommonBGBLL _commonBGBLL;
    private readonly Dictionary<int, BGConfig> _siteConfigDic;

    private readonly int _tpId = (int)Tp.BG;
    private readonly int _timeZone = -4;  //BG Default TimeZone=-4
    private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";//2021-09-28 23:59:59

    public WagerBGBLL(IServiceProvider provider)
    {
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        _wagerBGService = provider.GetRequiredService<WagerBGService>();
        _commonBGBLL = provider.GetRequiredService<CommonBGBLL>();
        _logger = provider.GetRequiredService<ILogger<WagerBGBLL>>();
        _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<BGConfig>(Tp.BG.ToString());
    }

    public async Task<bool> ExecWagerWorker(int siteId, string tpCurrencyId, WagerTimerType type, int timeRange, int needQuickTime)
    {
        var result = true;
        var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(_tpId, (int)type, tpCurrencyId, siteId);
        var quick = CheckQuick(dataTpTimer != null ? dataTpTimer.StartTime : DateTime.Now.AddMinutes(-10), needQuickTime);
        var time = GetTime(timeRange, _timeZone, _timeFormat, dataTpTimer != null ? dataTpTimer.StartTime : DateTime.Now.AddMinutes(-10), 5);

        if (type == WagerTimerType.Wager)
            result = await GetWagersAsync(siteId, tpCurrencyId, time.startTime, time.endTime, type);
        else if (type == WagerTimerType.RedoWager)
        {
            var cvTime = DateTimeOffset.Now.AddMinutes(-30).ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                result = await GetWagersAsync(siteId, tpCurrencyId, time.startTime, time.endTime, type, true);
            }
            else
            {
                result = false;
            }
        }

        if (result)
            _thirdPartyBLL.SetThirdPartyTimerByCurrency(_tpId, (int)type, time.endUnix, tpCurrencyId, siteId);

        return quick;
    }

    public async Task<bool> GetWagersAsync(int siteId, string tpCurrencyId, string startTime, string endTime, WagerTimerType type, bool isRedo = false)
    {
        var prefixMsg = $"{type} {siteId} {tpCurrencyId} ";
        var msg = prefixMsg + "FATAL_ERROR";
        try
        {
            // get tp info for api
            var config = _siteConfigDic[siteId];
            var sn = config.CurrencyData[tpCurrencyId].Sn;
            var secretCode = config.CurrencyData[tpCurrencyId].AgentPassword;
            var agentLoginId = config.CurrencyData[tpCurrencyId].AgentLoginId;

            // count interval wagers
            WagerBGTotalModel wagerSummary = null;
            if (isRedo)
                wagerSummary = _wagerBGService.GetWagerBGTotal(siteId, tpCurrencyId, startTime, endTime);

            var data = await _commonBGBLL.DoOrderQuery(config.ApiUrl, sn, secretCode, agentLoginId, startTime, endTime, prefixMsg, wagerSummary);

            if (data != null && data.Any())
            {
                data = data.OrderBy(x => x.orderId).ThenBy(x => x.lastUpdateTime);
                try
                {
                    await _wagerBGService.InsertWager(siteId, tpCurrencyId, data);
                }
                catch (Exception ex)
                {
                    msg = prefixMsg + $"INSERT_WAGER";
                    _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(data)}\r\n{ex.Message}");
                    return false;
                }
            }

            msg = prefixMsg + "FINISH";
            _logger.LogInformation($"{type} GetWagersAsync SiteId={siteId}. CurrencyId={tpCurrencyId}. {msg}\r\n{startTime} ~ {endTime}");
            return true;
        }
        catch (Exception ex)
        {
            msg = prefixMsg + "FATAL_ERROR";
            _logger.LogError(ex, $"{type} GetWagersAsync SiteId={siteId}. CurrencyId={tpCurrencyId}. {msg}\r\n{ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 檢查是否需要加速執行wager
    /// </summary>
    /// <param name="workTime"></param>
    /// <param name="needQuickTime"></param>
    /// <returns></returns>
    private bool CheckQuick(DateTime workTime, int needQuickTime)
    {
        var diff = DateTime.Now - workTime;
        return DateTime.Now > workTime && diff.TotalMinutes > needQuickTime;
    }

    //1. 为了避免采集到未结算注单，我们建议采集5分钟之前的注单数据;
    //2. 如果采集到未结算的注单，我们建议间隔3分钟后再次指定注单号(orderId)进行查询，只到返回正确的结果;
    //3. 为了确保高效地采集注单数据，我们建议每分钟采集一次数据，
    //   每次采集的记录数量不超过500条(即pageSize设定为500或以内)，
    //   如果指定时间范围内符合条件的注单数量(通过返回参数total判断符合条件的注单总数)超过pageSize的限制，
    //   请立即指定下一页的pageIndex再次发起查询请求，只到符合检索条件的注单悉数返回为止；
    //4. 查询时间条件示例: 01:00分采集00:54:00~00:54:59之间的数据，01:01分采集00:55:00~00:55:59的数据，以此类推
    private TimeCast GetTime(int period, int timezone, string format, DateTime timerTime, int beforeMin = 0, int utcMinute = 0)
    {
        var current = timerTime;

        var timeBase = new DateTime(
            current.Year,
            current.Month,
            current.Day,
            current.Hour,
            current.Minute,
            0
        );

        var dEndTime = timeBase.AddMinutes(period + 1).AddMilliseconds(-1);

        if (dEndTime > DateTime.Now)
        {
            dEndTime = new DateTime(
               DateTime.Now.Year,
               DateTime.Now.Month,
               DateTime.Now.Day,
               DateTime.Now.Hour,
               DateTime.Now.Minute,
               0
            );

            dEndTime.AddMilliseconds(-1);
        }

        var diff = DateTime.Now - dEndTime;

        if (diff.TotalMinutes <= beforeMin)
            dEndTime = dEndTime.AddMinutes(-1 * beforeMin);

        var beginAt = new DateTimeOffset(timeBase);
        var endAt = new DateTimeOffset(dEndTime);
        var startTime = beginAt.ToOffset(new TimeSpan(timezone, utcMinute, 0)).ToString(format);
        var endTime = endAt.ToOffset(new TimeSpan(timezone, utcMinute, 0)).ToString(format);
        var startUnix = beginAt.ToUnixTimeSeconds();
        var endUnix = endAt.ToUnixTimeSeconds();

        var timeCast = new TimeCast
        {
            startTime = startTime,
            endTime = endTime,
            startUnix = startUnix,
            endUnix = endUnix
        };

        return timeCast;
    }
}
