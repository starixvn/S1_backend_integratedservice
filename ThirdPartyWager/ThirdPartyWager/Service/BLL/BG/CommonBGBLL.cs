using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using Ubiety.Dns.Core;

namespace ThirdPartyWager.Service.BLL;

public class CommonBGBLL
{
    private readonly ApiBGBLL _apiBGBLL;
    private readonly ILogger<WagerBGBLL> _logger;
    private readonly int pageSize = 500;

    private readonly int _tpId = (int)Tp.BG;
    private readonly int _timeZone = -4;  //BG Default TimeZone=-4
    private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";//2021-09-28 23:59:59

    public CommonBGBLL(IServiceProvider provider)
    {
        _apiBGBLL = provider.GetRequiredService<ApiBGBLL>();
        _logger = provider.GetRequiredService<ILogger<WagerBGBLL>>();
    }

    internal bool GetSnAndSecretKey(Dictionary<string, string> setting, string tpCurrencyId, out string sn, out string secretKey, out string message)
    {
        sn = string.Empty;
        secretKey = string.Empty;
        message = string.Empty;

        if (!setting.ContainsKey($"Sn_{tpCurrencyId}")
            || string.IsNullOrWhiteSpace(setting[$"Sn_{tpCurrencyId}"]))
        {
            message = $"Sn_{tpCurrencyId} is invaild.";
            return false;
        }
        sn = setting[$"Sn_{tpCurrencyId}"];

        if (!setting.ContainsKey($"ApiSecretKey_{tpCurrencyId}")
            || string.IsNullOrWhiteSpace(setting[$"ApiSecretKey_{tpCurrencyId}"]))
        {
            message = $"ApiSecretKey_{tpCurrencyId} is invaild.";
            return false;
        }
        secretKey = setting[$"ApiSecretKey_{tpCurrencyId}"];

        return true;
    }

    internal async Task<IEnumerable<WagerBG_OrderQueryItemsModel>> DoOrderQuery(string apiUrl, string sn, string secrectKey,
        string startTime, string endTime, string prefixMsg, WagerBGTotalModel wagerSummary = null)
    {
        var msg = "FATAL_ERROR";
        // pagination
        var pageIndex = 1;
        var totalPage = 1;
        var isCheckTotal = wagerSummary != null;  //是否要檢查筆數是否正確
        var data = new List<WagerBG_OrderQueryItemsModel>();
        while (pageIndex <= totalPage || isCheckTotal)
        {
            // get wager
            var oederQueryResult = await _apiBGBLL.OrderQuery(apiUrl, sn, secrectKey, startTime, endTime, pageIndex, pageSize);

            if (oederQueryResult.error != null)
            {
                msg = prefixMsg + "THIRD_PARTY_ERROR";
                _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(oederQueryResult)}");
                return null;
            }

            var itemsResult = JsonConvert.DeserializeObject<WagerBG_OrderQueryResultResponseModel>(oederQueryResult.result.ToString());

            if (isCheckTotal)  //RedoWager Flow
            {
                // db wager count equals api bet count do nothing
                var apiTotal = new WagerBGTotalModel
                {
                    TotalCount = itemsResult.total,
                    Stake = itemsResult.items.Sum(x => x.bAmount),
                    ValidBet = itemsResult.items.Sum(x => x.validBet),
                    Payment = itemsResult.items.Sum(x => x.aAmount ?? 0),
                    WinLostAmount = itemsResult.items.Sum(x => x.payment),
                };
                if (itemsResult.total == wagerSummary.TotalCount)
                {
                    msg = prefixMsg + "MATCH";
                    _logger.LogInformation($"{msg}\r\nApi Summary={JsonConvert.SerializeObject(apiTotal)}\r\nWager Summary={JsonConvert.SerializeObject(wagerSummary)}");
                    isCheckTotal = false;
                    return null;
                }
                else
                {
                    msg = prefixMsg + "NOT_MATCH";
                    _logger.LogError($"{msg}\r\nApi Summary={JsonConvert.SerializeObject(apiTotal)}\r\nWager Summary={JsonConvert.SerializeObject(wagerSummary)}\r\nApi Data={JsonConvert.SerializeObject(itemsResult.items)}");
                }

                isCheckTotal = false;
            }

            totalPage = itemsResult.total / pageSize;
            if (itemsResult.total == 0)
            {
                msg = prefixMsg + "NO_DATA";
                _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(oederQueryResult)}");
                return null;
            }

            data.AddRange(itemsResult.items);

            pageIndex ++;
        }
        return data;
    }
}
