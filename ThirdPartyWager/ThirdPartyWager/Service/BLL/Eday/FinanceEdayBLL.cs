using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class FinanceEdayBLL :ITpReportBLL
    {
        private readonly ILogger<FinanceNTConnectBLL> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;
        private readonly ApiNTConnectBLL _apiNTConnectBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly string _tpName = Tp.Eday.ToString();
        private readonly NTConnectConfig _siteNTConnectConfig;
        private readonly int _timeZone = 0;

        public FinanceEdayBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceNTConnectBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiNTConnectBLL = provider.GetRequiredService<ApiNTConnectBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _siteNTConnectConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<NTConnectConfig>(_tpName);
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

        public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            var reportData = new List<dynamic>();
            
            var tasks = new List<Task<IEnumerable<FincanceDailySummary>>>
            {
                _commonBLL.GetWagerSummary("Finance_TpWager_93Connect_Summary_Get", reportTime.startUnix, reportTime.endUnix),
            };

            await Task.WhenAll(tasks);

            foreach (var item in tasks)
            {
                reportData.AddRange(item.Result);
            }
            return reportData;

        }

        public IEnumerable<FincanceDailySummary> DealReport(int tpId, DateTime reportDate, List<dynamic> reportData)
        {
            var dailyData = new List<FincanceDailySummary>();
            if (!reportData.Any())
            {
                return dailyData;
            }

            dailyData.AddRange(reportData.Select(item => new FincanceDailySummary()
            {
                SummaryType = SummaryType.TpBetRecord,
                SummaryDate = reportDate,
                TpId = tpId,
                CurrencyId = item.CurrencyId,
                Stake = item.Stake,
                ValidBet = item.ValidBet,
                Payout = item.Payout,
                WinlostAmount = item.WinlostAmount
            }));

            return dailyData;
        }
    }
}