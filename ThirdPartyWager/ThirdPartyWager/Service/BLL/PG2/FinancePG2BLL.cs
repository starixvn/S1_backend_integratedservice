using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL;

public class FinancePG2BLL : ITpReportBLL
{
    private readonly ILogger<FinancePG2BLL> _logger;
    private readonly CommonBLL _commonBLL;
    private readonly ApiPG2BLL _apiPG2BLL;
    private readonly DateTimeHelper _dateTimeHelper;
    private readonly int _timeZone = 0; // 報表帶入參數為日期格式，對方API預設+8
    private readonly int _gameType = (int)GameType.PG2_RNG;
    private readonly int _tpId = (int)Tp.PG2;
    private readonly IEnumerable<TpCurrency> _tpCurrencyList;
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly Dictionary<int, PG2Config> _siteConfigDic;

    public FinancePG2BLL(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<FinancePG2BLL>>();
        _commonBLL = provider.GetRequiredService<CommonBLL>();
        _apiPG2BLL = provider.GetRequiredService<ApiPG2BLL>();
        _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        _tpCurrencyList = _commonBLL.GetTpCurrencyList(_tpId);
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<PG2Config>(Tp.PG2.ToString());
    }
    public IEnumerable<FincanceDailySummary> DealReport(int tpId, DateTime reportDate, List<dynamic> reportData)
    {
        var dailyData = new List<FincanceDailySummary>();
        if (!reportData.Any())
        {
            return dailyData;
        }

        var _currencyId = _tpCurrencyList.ToDictionary(i => i.TpCurrencyId, i => i.CurrencyId);

        var getReportData = JsonConvert.DeserializeObject<List<CurrencyStatsItem_PG2>>(JsonConvert.SerializeObject(reportData));
        var data = getReportData.GroupBy(i => i.TpCurrencyId).Select(g => new
        {
            TpCurrencyId = g.Key,
            bet = g.Sum(s => s.bet),
            turnover = g.Sum(s => s.bet),
            winlose = g.Sum(s => s.winlose),
        });

        foreach (var item in data)
        {
            dailyData.Add(new FincanceDailySummary()
            {
                SummaryType = SummaryType.TpBetRecord,
                SummaryDate = reportDate,
                TpId = tpId,
                CurrencyId = _currencyId[item.TpCurrencyId],
                Stake = item.bet,
                ValidBet = item.turnover,
                Payout = item.bet + item.winlose,
                WinlostAmount = item.winlose,
            });
        }

        return dailyData;
    }

    public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
    {
        var reportData = new List<dynamic>();
        var siteList = _thirdPartyBLL.GetTpListByTpId(tpId);

        foreach (var site in siteList)
        {
            if (site.TicketKey == 0)
            {
                continue;
            }

            // 分Site取得TpList
            setting = _siteConfigDic[site.SiteId];

            foreach (var item in _tpCurrencyList)
            {
                var currency = item.TpCurrencyId;

                // 幣別apiKey有設定才會繼續做
                var apiKey = default(dynamic);
                try
                {
                    apiKey = setting.CurrencyData[currency].ApiKey; ;
                }
                catch
                {
                    continue;
                }

                var page = 1;
                int totalPage = 1;
                var perPage = 500;
                while (page <= totalPage)
                {
                    CurrencyStats_PG2 getWagerSummary = await _apiPG2BLL.GetBetSummaryByDate(setting, currency, _gameType, reportTime.startTime, reportTime.endTime, perPage, page);
                    if (getWagerSummary.status.code != 0)
                    {
                        var msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\n{_gameType}\r\n{currency}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                        SystemException systemException = new SystemException($"{msg} - GetReport({tpId})");
                        throw systemException;
                    }

                    if (getWagerSummary.status.code == 0 && getWagerSummary.data.transactions == null && getWagerSummary.data.totalPageAmount == 0)
                    {
                        var msg = "NO_DATA";
                        _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                        continue;
                    }
                    totalPage = (getWagerSummary.data.totalPageAmount / perPage) + 1;

                    foreach (var dataList in getWagerSummary.data.transactions)
                    {
                        dataList.TpCurrencyId = currency;
                        reportData.Add(dataList);
                    }
                    page++;
                }
            }
        }
        return reportData;
    }

    public TimeCast GetReportTime(DateTime startTime, int day)
    {
        var reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd", startTime);
        var cvTime = DateTimeOffset.Now.AddDays(day).ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
        if (reportTime.endUnix > cvTime)
        {
            reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd", startTime.AddDays(-1));
        }
        return reportTime;
    }
}