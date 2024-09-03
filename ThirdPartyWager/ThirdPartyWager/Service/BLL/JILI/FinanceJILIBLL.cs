using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class FinanceJILIBLL : ITpReportBLL
    {
        private readonly ILogger<FinanceJILIBLL> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;
        private readonly ApiJILIBLL _apiJILIBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _timeZone = -4;
        private readonly Dictionary<int, JILIConfig> _siteConfigDic;

        public FinanceJILIBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceJILIBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiJILIBLL = provider.GetRequiredService<ApiJILIBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<JILIConfig>(Tp.JILI.ToString());
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

            var getReportData = JsonConvert.DeserializeObject<List<CurrencyStatsData_JILI>>(JsonConvert.SerializeObject(reportData));
            var data = getReportData.GroupBy(i => i.Currency).Select(g => new
            {
                Currency = g.Key,
                BetAmount = g.Sum(s => s.BetAmount),
                PayoffAmount = g.Sum(s => s.PayoffAmount),
            });

            foreach (var item in data)
            {
                dailyData.Add(new FincanceDailySummary()
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = reportDate,
                    TpId = tpId,
                    CurrencyId = _currencyId[item.Currency],
                    Stake = item.BetAmount,
                    ValidBet = item.BetAmount,
                    Payout = item.PayoffAmount,
                    WinlostAmount = item.BetAmount + item.PayoffAmount
                });
            }

            return dailyData;
        }

        public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            var reportData = new List<dynamic>();
            var siteList = _thirdPartyBLL.GetTpListByTpId(tpId);
            foreach (var siteData in siteList)
            {
                if (siteData.TicketKey == 0)
                {
                    _logger.LogError($"SiteId={siteData.SiteId}, no execute");
                    continue;
                }

                // 分Site取得SiteConfig
                setting = _siteConfigDic[siteData.SiteId];

                var getWagerSummary = await _apiJILIBLL.GetBetSummaryByTime(setting, reportTime.startTime, reportTime.endTime);
                if (getWagerSummary.ErrorCode == 101)
                {
                    var msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\nSiteId={siteData.SiteId}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    continue;
                }

                if (getWagerSummary.ErrorCode != 0)
                {
                    var msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\nSiteId={siteData.SiteId}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    SystemException systemException = new SystemException($"{msg} - GetReport({tpId})");
                    throw systemException;
                }

                foreach (var item in getWagerSummary.Data)
                {
                    reportData.Add(item);
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
    }
}
