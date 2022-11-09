using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class FinanceMPBLL : ITpReportBLL
    {
        private readonly ILogger<FinanceMPBLL> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;
        private readonly ApiMPBLL _apiMPBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _timeZone = 8; // 因為API帶入的時間格式，且對應MP GameEndTime參數(MP 為+8)，故API時區調整為+8

        public FinanceMPBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceMPBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiMPBLL = provider.GetRequiredService<ApiMPBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        }

        public IEnumerable<FincanceDailySummary> DealReport(int tpId, DateTime reportDate, List<dynamic> reportData)
        {
            var dailyData = new List<FincanceDailySummary>();
            if (!reportData.Any())
            {
                return dailyData;
            }

            // 目前僅支援THB
            foreach (var item in reportData)
            {
                dailyData.Add(new FincanceDailySummary()
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = reportDate,
                    TpId = tpId,
                    CurrencyId = 4,
                    Stake = item.totalBetAmount,
                    ValidBet = item.totalBetAmount,
                    Payout = item.totalWinAmount,
                    WinlostAmount = item.playerPL
                });
            }

            return dailyData;
        }

        public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            var tpCurrencyList = _commonBLL.GetTpCurrencyList(tpId);
            var reportData = new List<dynamic>();

            CurrencyStats_MP getWagerSummary = await _apiMPBLL.GetBetSummaryByTime(setting, reportTime.startTime, reportTime.endTime);

            if ((getWagerSummary.d.code != 0 && getWagerSummary.d.code != 16) || getWagerSummary.d.msg != null)
            {
                var msg = "THIRD_PARTY_ERROR";
                _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                SystemException systemException = new SystemException($"{msg} - GetReport({tpId})");
                throw systemException;
            }

            if (getWagerSummary.d.code == 16 || getWagerSummary.d.Transactions[0].betCount == 0)
            {
                var msg = "NO_DATA";
                _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                return reportData;
            }

            reportData.AddRange(getWagerSummary.d.Transactions);
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
