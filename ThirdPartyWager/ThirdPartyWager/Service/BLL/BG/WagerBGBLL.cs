using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Generic;
using System.Security.Policy;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL;

public class WagerBGBLL
{
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly WagerBGService _wagerBGService;
    private readonly ILogger<WagerBGBLL> _logger;
    private readonly DateTimeHelper _dateTimeHelper;
    private readonly CommonBGBLL _commonBGBLL;

    private readonly int _tpId = (int)Tp.BG;
    private readonly int _timeZone = -4;  //BG Default TimeZone=-4
    private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";//2021-09-28 23:59:59

    public WagerBGBLL(IServiceProvider provider)
    {
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        _wagerBGService = provider.GetRequiredService<WagerBGService>();
        _commonBGBLL = provider.GetRequiredService<CommonBGBLL>();
        _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        _logger = provider.GetRequiredService<ILogger<WagerBGBLL>>();
    }

    public async Task<bool> ExecWagerWorker(int siteId, string tpCurrencyId, WagerTimerType type, int timeRange, int needQuickTime)
    {
        var result = true;
        var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(_tpId, (int)type, tpCurrencyId, siteId);
        var quick = CheckQuick(dataTpTimer != null ? dataTpTimer.StartTime : DateTime.Now, needQuickTime);
        var time = _dateTimeHelper.GetTimeByMinute(timeRange, _timeZone, _timeFormat, dataTpTimer != null ? dataTpTimer.StartTime : DateTime.Now);

        if (type == WagerTimerType.Wager)
            result = await GetWagersAsync(siteId, tpCurrencyId, time.startTime, time.endTime, type);
        else if (type == WagerTimerType.RedoWager)
        {
            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
                result = await GetWagersAsync(siteId, tpCurrencyId, time.startTime, time.endTime, type, true);

        }

        time = _dateTimeHelper.GetTimeByMinute(timeRange, 0, _timeFormat, dataTpTimer != null ? dataTpTimer.StartTime : DateTime.Now);
        if (result)
            _thirdPartyBLL.SetThirdPartyTimerByCurrency(_tpId, (int)type, time.endUnix, tpCurrencyId, siteId);

        return quick;
    }

    public async Task<bool> GetWagersAsync(int siteId, string tpCurrencyId, string startTime, string endTime,WagerTimerType type, bool isRedo = false)
    {
        var prefixMsg = $"{type} {siteId} {tpCurrencyId}_";
        var msg = prefixMsg + "FATAL_ERROR";
        try
        {
            // get tp info for api
            var setting = _thirdPartyBLL.GetThirdPartyBySite(_tpId, siteId);

            if (!_commonBGBLL.GetSnAndSecretKey(setting, tpCurrencyId,
                out string sn, out string secretKey, out string secretCode, out string agentLoginId, out string message))
            {
                msg = prefixMsg + "THIRD_PARTY_SETTING_ERROR";
                _logger.LogError($"{msg} {message}");
                return false;
            }

            // count interval wagers
            WagerBGTotalModel wagerSummary = null;
            if (isRedo)
                wagerSummary = _wagerBGService.GetWagerBGTotal(siteId, tpCurrencyId, startTime, endTime);

            var data = await _commonBGBLL.DoOrderQuery(setting["ApiUrl"], sn, secretCode, agentLoginId, startTime, endTime, prefixMsg, wagerSummary);

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

    private bool CheckQuick(DateTime workTime, int needQuickTime)
    {
        var diff = DateTime.Now - workTime;
        return DateTime.Now > workTime && diff.TotalMinutes > needQuickTime;
    }
}
