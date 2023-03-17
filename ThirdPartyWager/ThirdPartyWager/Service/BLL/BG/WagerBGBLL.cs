using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Cms;
using System;
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

    public async Task<bool> ExecWagerWorker(int siteId, string tpCurrencyId, int type, int timeRange, int needQuickTime)
    {
        var result = true;
        var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(_tpId, type, tpCurrencyId, siteId);
        var quick = CheckQuick(dataTpTimer != null ? dataTpTimer.StartTime : DateTime.Now, needQuickTime);
        var time = _dateTimeHelper.GetTimeByMinute(timeRange, _timeZone, _timeFormat, dataTpTimer != null ? dataTpTimer.StartTime : DateTime.Now);

        if (type == (int)WagerTimerType.Wager)
            result = await GetWagersAsync(siteId, tpCurrencyId, time.startTime, time.endTime);
        else if (type == (int)WagerTimerType.RedoWager)
        {
            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                // count interval wagers
                var wagerSummary = _wagerBGService.GetWagerBGTotal(time.startTime, time.endTime);
                result = await GetWagersAsync(siteId, tpCurrencyId, time.startTime, time.endTime, wagerSummary);
            }
        }

        if (result)
            _thirdPartyBLL.SetThirdPartyTimerByCurrency(_tpId, type, time.endUnix, tpCurrencyId, siteId);

        return quick;
    }

    public async Task<bool> GetWagersAsync(int siteId, string tpCurrencyId, string startTime, string endTime, WagerBGTotalModel wagerSummary = null)
    {
        var prefixMsg = $"{siteId}_{tpCurrencyId}_";
        var msg = prefixMsg + "FATAL_ERROR";
        try
        {
            // get tp info for api
            var setting = _thirdPartyBLL.GetThirdPartyBySite(_tpId, siteId);

            if (!_commonBGBLL.GetSnAndSecretKey(setting, tpCurrencyId, out string sn, out string secretKey, out string message))
            {
                msg = prefixMsg + "THIRD_PARTY_SETTING_ERROR";
                _logger.LogError($"{msg} {message}");
                return false;
            }

            var data = await _commonBGBLL.DoOrderQuery(setting["ApiUrl"], sn, secretKey, startTime, endTime, prefixMsg, wagerSummary);

            if (data != null && data.Any())
            {
                try
                {
                    await _wagerBGService.InsertWager(data);
                }
                catch (Exception ex)
                {
                    msg = prefixMsg + $"INSERT_WAGER";
                    _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(data)}\r\n{ex.Message}");
                    return false;
                }
            }

            msg = prefixMsg + "FINISH";
            _logger.LogInformation($"GetWagersAsync SiteId={siteId}. CurrencyId={tpCurrencyId}. {msg}\r\n{startTime} ~ {endTime}");
            return true;
        }
        catch (Exception ex)
        {
            msg = prefixMsg + "FATAL_ERROR";
            _logger.LogError(ex, $"GetWagersAsync SiteId={siteId}. CurrencyId={tpCurrencyId}. {msg}\r\n{ex.Message}");
            return false;
        }
    }

    private bool CheckQuick(DateTime workTime, int needQuickTime)
    {
        var diff = DateTime.Now - workTime;
        return DateTime.Now > workTime && diff.TotalMinutes > excess;
    }
}
