using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;
namespace ThirdPartyWager.Service.BLL;

public class FinanceBGBLL : ITpReportBLL
{
    private readonly ILogger<FinanceEVOBLL> _logger;
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly CommonBLL _commonBLL;
    private readonly ApiBGBLL _apiBGBLL;
    private readonly CommonBGBLL _commonBGBLL;
    private readonly DateTimeHelper _dateTimeHelper;
    private readonly Dictionary<int, BGConfig> _siteConfigDic;
    private readonly int _timeZone = -4;

    public FinanceBGBLL(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<FinanceEVOBLL>>();
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        _commonBLL = provider.GetRequiredService<CommonBLL>();
        _apiBGBLL = provider.GetRequiredService<ApiBGBLL>();
        _commonBGBLL = provider.GetRequiredService<CommonBGBLL>();
        _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<BGConfig>(Tp.BG.ToString());
    }

    public IEnumerable<FincanceDailySummary> DealReport(int tpId, DateTime reportDate, List<dynamic> reportData)
    {
        var dailyData = new List<FincanceDailySummary>();
        if (!reportData.Any())
            return dailyData;

        var tpCurrencyList = _commonBLL.GetTpCurrencyList(tpId);

        foreach (var item in tpCurrencyList)
        {
            var sumData = Sum(JsonConvert.SerializeObject(reportData.Where(x => x.currencyId == item.CurrencyId).Select(x => x.dataResult)));

            if (sumData.orderamount == 0 && sumData.validbetamount == 0 && sumData.paymentamount == 0) continue;

            dailyData.Add(new FincanceDailySummary()
            {
                SummaryType = SummaryType.TpBetRecord,
                SummaryDate = reportDate,
                TpId = tpId,
                CurrencyId = item.CurrencyId,
                Stake = sumData.orderamount,
                ValidBet = sumData.validbetamount,
                Payout = sumData.paymentamount + sumData.validbetamount,
                WinlostAmount = sumData.paymentamount

            });
        }
        return dailyData;
    }

    public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
    {
        var siteList = _thirdPartyBLL.GetTpListByTpId(tpId);
        var tpCurrencyList = _commonBLL.GetTpCurrencyList(tpId);
        var reportData = new List<dynamic>();

        foreach (var siteInfo in siteList)
        {
            setting = _siteConfigDic[siteInfo.SiteId];
            if (setting == null) continue;

            foreach (var item in tpCurrencyList)
            {
                if (!setting.CurrencyData.ContainsKey(item.TpCurrencyId))
                {
                    continue;
                }
                var currencyData = setting.CurrencyData[item.TpCurrencyId];

                var prefixMsg = $"Finance {siteInfo.SiteId}_{item.TpCurrencyId}_";
                var sn = currencyData.Sn;
                var secretCode = currencyData.AgentPassword;
                var agentLoginId = currencyData.AgentLoginId;
                var secrectKey = currencyData.ApiSecretKey;

                var getSummaryResult = await _apiBGBLL.OrderSum(setting.ApiUrl, sn, secrectKey, agentLoginId, reportTime.startTime, reportTime.endTime);
                if (getSummaryResult.error != null)
                {
                    _logger.LogError($"{prefixMsg}THIRD_PARTY_ERROR\r\n{JsonConvert.SerializeObject(getSummaryResult)}");
                    throw new SystemException($"{prefixMsg}THIRD_PARTY_ERROR - GetReport({tpId})");
                }

                var itemsResult = JsonConvert.DeserializeObject<WagerBG_OrderSumResultResponseModel>(getSummaryResult.result.ToString());

                if (itemsResult.total == 0)
                {
                    _logger.LogInformation($"{prefixMsg}NO_DATA\r\n{JsonConvert.SerializeObject(itemsResult)}");
                    continue;
                }

                reportData.Add(new { siteId = siteInfo.SiteId, currencyId = item.CurrencyId, dataResult = itemsResult.stats });
            }
        }

        return reportData;
    }

    public TimeCast GetReportTime(DateTime startTime, int day)
    {
        var reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd HH:mm:ss", startTime);
        var cvTime = DateTimeOffset.Now.AddDays(day).ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
        if (reportTime.endUnix > cvTime)
        {
            reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd HH:mm:ss", startTime.AddDays(-1));
        }
        return reportTime;
    }

    private WagerBG_OrderSumStatsModel Sum(string reportData)
    {
        var sourceTypeDatas = JsonConvert.DeserializeObject<IEnumerable<WagerBG_OrderSumStatsModel>>(reportData);
        return new WagerBG_OrderSumStatsModel
        {
            orderamount = sourceTypeDatas.Sum(x => x.orderamount),
            paymentamount = sourceTypeDatas.Sum(x => x.paymentamount),
            usercount = sourceTypeDatas.Sum(x => x.usercount),
            validorderamount = sourceTypeDatas.Sum(x => x.validorderamount),
            validbetamount = sourceTypeDatas.Sum(x => x.validbetamount),
            ordercount = sourceTypeDatas.Sum(x => x.ordercount),
        };
    }
}