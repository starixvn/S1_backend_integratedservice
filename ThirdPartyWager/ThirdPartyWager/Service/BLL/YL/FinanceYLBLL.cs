using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class FinanceYLBLL : ITpReportBLL
    {
        private readonly ILogger<FinanceYLBLL> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;
        private readonly ApiYLBLL _apiYLBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _timeZone = 0;//轉換為+0800
        private readonly Dictionary<string, CurrencyOption> _isoCodeList;

        public FinanceYLBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceYLBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiYLBLL = provider.GetRequiredService<ApiYLBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _isoCodeList = provider.GetRequiredService<IEnumerable<CurrencyOption>>().ToDictionary(d => d.IsoCode, d => d);
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

            var getReportData = JsonConvert.DeserializeObject<List<CurrencyStatsData_YL>>(JsonConvert.SerializeObject(reportData));

            foreach (var item in getReportData)
            {
                dailyData.Add(new FincanceDailySummary()
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = reportDate,
                    TpId = tpId,
                    CurrencyId = _isoCodeList[item.currency].CurrencyId,
                    Stake = item.totalBetAmount,
                    ValidBet = item.totalValidBet,
                    Payout = item.totalWinAmount,
                    WinlostAmount = item.totalWinLoss,
                    ProgressiveBet = 0,
                    ProgressiveWin = 0
                });
            }
            return dailyData;
        }

        public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            var reportData = new List<dynamic>();
            CurrencyStats_YL getWagerSummary = await _apiYLBLL.GetBetSummaryByTime(setting, reportTime.startTime, reportTime.endTime);

            // error condition
            var errorCode = getWagerSummary.status;
            if (errorCode == "1003")
            {
                var msg = "NO_DATA";
                _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                return reportData;
            }
            if (errorCode != "1")
            {
                var msg = "THIRD_PARTY_ERROR";
                _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                SystemException systemException = new SystemException($"{msg} - GetReport({tpId})");
                throw systemException;
            }

            foreach (var item in getWagerSummary.items)
            {
                reportData.Add(item);
            }

            return reportData;
        }

        public TimeCast GetReportTime(DateTime startTime, int day)
        {
            // 取整點結束時間，轉換為+0800
            var reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd'T'HH:mm:ss.fff+0800", startTime, 0, 0);
            var cvTime = DateTimeOffset.Now.AddDays(day).ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (reportTime.endUnix > cvTime)
            {
                reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd'T'HH:mm:ss.fff+0800", startTime.AddDays(-1), 0, 0);
            }
            return reportTime;
        }
    }
}
