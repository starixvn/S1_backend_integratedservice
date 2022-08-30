using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class FinanceYGGBLL : ITpReportBLL
    {
        private readonly ILogger<FinanceYGGBLL> _logger;
        private readonly CommonBLL _commonBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _timeZone = 0;
        private readonly IEnumerable<TpCurrency> _tpCurrencyList;
        private readonly int _tpId = (int)Tp.YGG;

        public FinanceYGGBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceYGGBLL>>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _tpCurrencyList = _commonBLL.GetTpCurrencyList(_tpId);
        }

        public IEnumerable<FincanceDailySummary> DealReport(int tpId, DateTime reportDate, List<dynamic> reportData)
        {
            var dailyData = new List<FincanceDailySummary>();
            if (!reportData.Any())
            {
                return dailyData;
            }

            var _currencyId = _tpCurrencyList.ToDictionary(i => i.TpCurrencyId, i => i.CurrencyId);

            var getReportData = JsonConvert.DeserializeObject<List<FincanceDailySummary>>(JsonConvert.SerializeObject(reportData));
            foreach (var item in getReportData)
            {
                dailyData.Add(new FincanceDailySummary()
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = reportDate,
                    TpId = tpId,
                    CurrencyId = _currencyId[item.TpCurrencyId],
                    Stake = item.Stake,
                    ValidBet = item.ValidBet,
                    Payout = item.Payout,
                    WinlostAmount = item.WinlostAmount
                });
            }

            return dailyData;
        }

        public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            var reportData = new List<dynamic>();
            var bettrans = await _commonBLL.GetWagerSummary("Finance_TpWagerYGG_Summary_Get", reportTime.startUnix, reportTime.endUnix);
            reportData.AddRange(bettrans);
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
