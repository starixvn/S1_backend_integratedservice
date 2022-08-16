using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class FinanceDGBLL : ITpReportBLL
    {
        private readonly ILogger<FinanceDGBLL> _logger;
        private readonly CommonBLL _commonBLL;
        private readonly CommonService _commonService;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _timeZone = 0;

        public FinanceDGBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceDGBLL>>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _commonService = provider.GetRequiredService<CommonService>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        }

        public IEnumerable<FincanceDailySummary> DealReport(int tpId, DateTime reportDate, List<dynamic> reportData)
        {
            var dailyData = new List<FincanceDailySummary>();
            if (!reportData.Any())
            {
                return dailyData;
            }

            var getReportData = JsonConvert.DeserializeObject<List<FincanceDailySummary>>(JsonConvert.SerializeObject(reportData));

            foreach (var item in getReportData)
            {
                dailyData.Add(new FincanceDailySummary()
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = reportDate,
                    TpId = tpId,
                    CurrencyId = item.CurrencyId,
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
            var bettrans = await _commonService.GetWagerSummary("Finance_TpWagerDG_Summary_Get", reportTime.startUnix, reportTime.endUnix);
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
