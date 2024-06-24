using Newtonsoft.Json;

using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL;

public class CommonBGBLL
{
    private readonly ApiBGBLL _apiBGBLL;
    private readonly ILogger<WagerBGBLL> _logger;
    private readonly int pageSize = 500;
    public CommonBGBLL(IServiceProvider provider)
    {
        _apiBGBLL = provider.GetRequiredService<ApiBGBLL>();
        _logger = provider.GetRequiredService<ILogger<WagerBGBLL>>();
    }

    internal async Task<IEnumerable<WagerBG_OrderQueryItemsModel>> DoOrderQuery(string apiUrl, string sn, string secretCode,
        string agentLoginId, string startTime, string endTime, string prefixMsg, WagerBGTotalModel wagerSummary = null)
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
            var oederQueryResult = await _apiBGBLL.OrderQuery(apiUrl, sn, secretCode, agentLoginId, startTime, endTime, pageIndex, pageSize);

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
                if (apiTotal.TotalCount == wagerSummary.TotalCount
                    && apiTotal.Stake == wagerSummary.Stake
                    && apiTotal.ValidBet == wagerSummary.ValidBet
                    && apiTotal.Payment == wagerSummary.Payment
                    && apiTotal.WinLostAmount == wagerSummary.WinLostAmount)
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
                _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(itemsResult.items)}");
                return null;
            }

            data.AddRange(itemsResult.items);

            pageIndex++;
        }
        return data;
    }
}
