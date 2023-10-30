using Newtonsoft.Json;

using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class FinanceTCGBLL : ITpReportBLL
    {
        private readonly ILogger<FinanceTCGBLL> _logger;
        private readonly CommonBLL _commonBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _timeZone = 0;
        private readonly int _tpId = (int)Tp.TCG;

        public FinanceTCGBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceTCGBLL>>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
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
            var data = getReportData.GroupBy(i => i.CurrencyId).Select(g => new
            {
                CurrencyId = g.Key,
                Stake = g.Sum(s => s.Stake),
                ValidBet = g.Sum(s => s.ValidBet),
                WinlostAmount = g.Sum(s => s.WinlostAmount),
            });

            foreach (var item in data)
            {
                dailyData.Add(new FincanceDailySummary()
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = reportDate,
                    TpId = tpId,
                    CurrencyId = item.CurrencyId,
                    Stake = item.Stake,
                    ValidBet = item.ValidBet,
                    WinlostAmount = item.WinlostAmount
                });
            }

            return dailyData;
        }

        public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            var reportData = new List<dynamic>();

            var tasks = new List<Task<IEnumerable<FincanceDailySummary>>>
            {
               _commonBLL.GetWagerSummary("Finance_TpWagerTCG_SEA_Summary_Get", reportTime.startUnix, reportTime.endUnix),
               _commonBLL.GetWagerSummary("Finance_TpWagerTCG_VN_Summary_Get", reportTime.startUnix, reportTime.endUnix)
            };

            await Task.WhenAll(tasks);

            foreach (var item in tasks)
            {
                reportData.AddRange(item.Result);
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
