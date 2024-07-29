using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class FinanceSGBLL : ITpReportBLL
    {
        private readonly ILogger<FinanceSGBLL> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;
        private readonly ApiSGBLL _apiSGBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _timeZone = 8;
        private readonly Dictionary<int, SGConfig> _siteConfigDic;


        public FinanceSGBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceSGBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiSGBLL = provider.GetRequiredService<ApiSGBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<SGConfig>(Tp.SG.ToString());
        }

        public IEnumerable<FincanceDailySummary> DealReport(int tpId, DateTime reportDate, List<dynamic> reportData)
        {
            var dailyData = new List<FincanceDailySummary>();
            if (!reportData.Any())
            {
                return dailyData;
            }

            var tpCurrencyList = _commonBLL.GetTpCurrencyList(tpId);
            var _currencyId = tpCurrencyList.ToDictionary(i => i.TpCurrencyId, i => i.CurrencyId);

            var getReportData = JsonConvert.DeserializeObject<List<CurrencyStatsData_SG>>(JsonConvert.SerializeObject(reportData));
            var data = getReportData.GroupBy(i => i.currency).Select(g => new
            {
                Currency = g.Key,
                betAmt = g.Sum(s => s.betAmt),
                winLoss = g.Sum(s => s.winLoss),
                bonusAmt = g.Sum(s => s.bonusAmt),
                jackpotAmount = g.Sum(s => s.jackpotAmount),
                jpWin = g.Sum(s => s.jpWin),
            });

            foreach (var item in getReportData)
            {
                dailyData.Add(new FincanceDailySummary()
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = reportDate,
                    TpId = tpId,
                    CurrencyId = _currencyId[item.currency],
                    Stake = item.betAmt,
                    ValidBet = item.betAmt,
                    Payout = item.betAmt + item.winLoss - item.bonusAmt,
                    WinlostAmount = item.winLoss - item.bonusAmt,
                    ProgressiveBet = item.jackpotAmount,
                    ProgressiveWin = item.jpWin
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
                if(site.TicketKey != 1)
                {
                    continue;
                }

                // 分Site取得TpList
                setting = _siteConfigDic[site.SiteId];

                CurrencyStats_SG getWagerSummary = await _apiSGBLL.GetBetSummaryByTime(setting, reportTime.startTime, reportTime.endTime);

                // error condition
                var errorCode = getWagerSummary.code;
                if (errorCode != 0)
                {
                    var msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\nSiteId={site.SiteId}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    SystemException systemException = new SystemException($"{msg} - GetReport({tpId})");
                    throw systemException;
                }
                if (errorCode == 0 && getWagerSummary.resultCount == 0)
                {
                    var msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\nSiteId={site.SiteId}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    return reportData;
                }

                foreach (var item in getWagerSummary.list)
                {
                    reportData.Add(item);
                }
            }

            return reportData;
        }

        public TimeCast GetReportTime(DateTime startTime, int day)
        {
            var reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyyMMdd'T'HHmmss", startTime);
            var cvTime = DateTimeOffset.Now.AddDays(day).ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (reportTime.endUnix > cvTime)
            {
                reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyyMMdd'T'HHmmss", startTime.AddDays(-1));
            }
            return reportTime;
        }
    }
}
