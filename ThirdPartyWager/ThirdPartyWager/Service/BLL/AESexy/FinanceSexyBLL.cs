using Newtonsoft.Json;

using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class FinanceSexyBLL : ITpReportBLL
    {
        private readonly ILogger<FinanceSexyBLL> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;
        private readonly ApiSexyBLL _apiSexyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _timeZone = 0;
        private readonly Dictionary<int, Dictionary<string, string>> _siteConfigDic;


        public FinanceSexyBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceSexyBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiSexyBLL = provider.GetRequiredService<ApiSexyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<Dictionary<string, string>>(Tp.AESexy.ToString());
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

            var getReportData = JsonConvert.DeserializeObject<List<CurrencyStatsData_Sexy>>(JsonConvert.SerializeObject(reportData));
            var data = getReportData.GroupBy(i => i.currency).Select(g => new
            {
                currency = g.Key,
                betAmount = g.Sum(s => s.betAmount),
                realBetAmount = g.Sum(s => s.realBetAmount),
                turnover = g.Sum(s => s.turnover),
                winAmount = g.Sum(s => s.winAmount),
                realWinAmount = g.Sum(s => s.realWinAmount),
                jackpotBetAmount = g.Sum(s => s.jackpotBetAmount),
                jackpotWinAmount = g.Sum(s => s.jackpotWinAmount),
            });

            foreach (var item in data)
            {
                dailyData.Add(new FincanceDailySummary()
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = reportDate,
                    TpId = tpId,
                    CurrencyId = _currencyId[item.currency],
                    Stake = item.betAmount,
                    ValidBet = item.turnover,
                    Payout = item.winAmount,
                    WinlostAmount = item.winAmount - item.betAmount
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

                setting = _siteConfigDic[site.SiteId];
                CurrencyStats_Sexy getWagerSummary = await _apiSexyBLL.GetBetSummaryByTime(setting, reportTime.startTime, reportTime.endTime);

                if (getWagerSummary.status != "0000")
                {
                    var msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    continue;
                }

                if (getWagerSummary.status == "0000" && !getWagerSummary.transactions.Any())
                {
                    var msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    continue;
                }

                foreach (var item in getWagerSummary.transactions)
                {
                    reportData.Add(item);
                }
            }
            return reportData;
        }

        public TimeCast GetReportTime(DateTime startTime, int day)
        {
            var reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd'T'HH:mm:ss.fffZ", startTime);
            var cvTime = DateTimeOffset.Now.AddDays(day).ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (reportTime.endUnix > cvTime)
            {
                reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd'T'HH:mm:ss.fffZ", startTime.AddDays(-1));
            }
            return reportTime;
        }
    }
}
