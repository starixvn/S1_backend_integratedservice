using Newtonsoft.Json;
using System;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL;

public class WagerBGBLL
{
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly ApiBGBLL _apiBGBLL;
    private readonly WagerBGService _wagerBGService;
    private readonly ILogger<WagerBGBLL> _logger;
    private readonly DateTimeHelper _dateTimeHelper;
    private readonly CommonBGBLL _commonBGBLL;
    private readonly int pageSize = 500;

    private readonly int _tpId = (int)Tp.BG;
    private readonly int _timeZone = -4;  //BG Default TimeZone=-4
    private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";//2021-09-28 23:59:59

    public WagerBGBLL(IServiceProvider provider)
    {
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        _apiBGBLL = provider.GetRequiredService<ApiBGBLL>();
        _wagerBGService = provider.GetRequiredService<WagerBGService>();
        _commonBGBLL = provider.GetRequiredService<CommonBGBLL>();
        _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        _logger = provider.GetRequiredService<ILogger<WagerBGBLL>>();
    }

    public async Task<bool> ExecWagerWorker(int siteId, string tpCurrencyId, int type, int timeRange)
    {
        var result = true;
        var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(_tpId, type, tpCurrencyId, siteId);
        var quick = CheckQuick(dataTpTimer != null ? dataTpTimer.StartTime : DateTime.Now, 5);
        var time = _dateTimeHelper.GetTimeByMinute(timeRange, _timeZone, _timeFormat, dataTpTimer != null ? dataTpTimer.StartTime : DateTime.Now);

        result = await GetWagersAsync(siteId, tpCurrencyId, time.startTime, time.endTime);
        if (result)
            _thirdPartyBLL.SetThirdPartyTimerByCurrency(_tpId, type, time.endUnix, tpCurrencyId, siteId);

        return quick;
    }

    public async Task<bool> GetWagersAsync(int siteId, string tpCurrencyId, string startTime, string endTime)
    {
        var prefixMsg = $"{siteId}_{tpCurrencyId}_";
        var msg = prefixMsg + "FATAL_ERROR";
        try
        {
            // get tp info for api
            var setting = _thirdPartyBLL.GetThirdPartyBySite(_tpId, siteId);

            if (!_commonBGBLL.GetSnAndSecretKey(setting, tpCurrencyId, out string sn, out string secretKey,out string message))
            {
                msg = prefixMsg + "THIRD_PARTY_SETTING_ERROR";
                _logger.LogError($"{msg} {message}");
                return false;
            }

            // pagination
            var pageIndex = 1;
            var totalPage = 1;
            while (pageIndex <= totalPage)
            {
                // get wager
                var oederQueryResult = await _apiBGBLL.OrderQuery(setting["ApiUrl"], sn, secretKey, startTime, endTime, pageIndex, pageSize);

                if (oederQueryResult.error != null)
                {
                    msg = prefixMsg + "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(oederQueryResult)}");
                    return false;
                }

                totalPage = oederQueryResult.result.total / pageSize;
                if (oederQueryResult.result.total == 0)
                {
                    msg = prefixMsg + "NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(oederQueryResult)}");
                    return true;
                }

                try
                {
                    await _wagerBGService.InsertWager(oederQueryResult.result.items);
                }
                catch (Exception ex)
                {
                    msg = prefixMsg + $"INSERT_WAGER";
                    _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(oederQueryResult.result.items)}\r\n{ex.Message}");
                    return false;
                }

                pageIndex++;
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

    private bool CheckQuick(DateTime workTime, int excess)
    {
        var diff = DateTime.Now - workTime;
        return DateTime.Now > workTime && diff.TotalMinutes > excess;
    }
}
