using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class FinanceAPBLL : ITpReportBLL
    {
        private readonly ILogger<FinanceAPBLL> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;
        private readonly ApiAPBLL _apiAPBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _timeZone = 0;
        private readonly List<int> _siteList = new List<int>() { 1, 2, 3 };

        public FinanceAPBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceAPBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiAPBLL = provider.GetRequiredService<ApiAPBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
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

            var getReportData = JsonConvert.DeserializeObject<List<CurrencyStatsData_AP>>(JsonConvert.SerializeObject(reportData));
            var data = getReportData.GroupBy(i => i.Currency).Select(g => new
            {
                Currency = g.Key,
                TotalStake = g.Sum(s => s.TotalStake),
                TotalWin = g.Sum(s => s.TotalWin),
            });

            foreach (var item in data)
            {
                dailyData.Add(new FincanceDailySummary()
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = reportDate,
                    TpId = tpId,
                    CurrencyId = _currencyId[item.Currency],
                    Stake = item.TotalStake,
                    ValidBet = item.TotalStake,
                    Payout = item.TotalWin,
                    WinlostAmount = item.TotalWin - item.TotalStake
                });
            }

            return dailyData;
        }

        public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            var reportData = new List<dynamic>();
            foreach (var siteId in _siteList)
            {
                // 分Site取得TpList
                setting = _thirdPartyBLL.GetThirdPartyBySite(tpId, siteId);

                var getWagerSummary = await _apiAPBLL.GetBetSummaryByTime(setting, reportTime.startTime, reportTime.endTime);

                if (getWagerSummary.ErrorCode != 0)
                {
                    var msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\nSiteId=\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    SystemException systemException = new SystemException($"{msg} - GetReport({tpId})");
                    throw systemException;
                }

                if (getWagerSummary.ErrorCode == 0 && getWagerSummary.Results == null)
                {
                    var msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\nSiteId={siteId}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    continue;
                }

                foreach (var item in getWagerSummary.Results)
                {
                    reportData.Add(item);
                }
            }
            return reportData;
        }

        public TimeCast GetReportTime(DateTime startTime, int day)
        {
            // 取整點結束時間
            var reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd'T'HH:mm:ss.ffffffzzz", startTime, 0, 0);
            var cvTime = DateTimeOffset.Now.AddDays(day).ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (reportTime.endUnix > cvTime)
            {
                reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd'T'HH:mm:ss.ffffffzzz", startTime.AddDays(-1), 0, 0);
            }
            return reportTime;
        }
    }
}
