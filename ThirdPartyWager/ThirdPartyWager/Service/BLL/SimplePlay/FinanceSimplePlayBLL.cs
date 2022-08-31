using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class FinanceSimplePlayBLL : ITpReportBLL
    {
        private readonly ILogger<FinanceSimplePlayBLL> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;
        private readonly ApiSimplePlayBLL _apiSimplePlayBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _timeZone = 8;

        public FinanceSimplePlayBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceSimplePlayBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiSimplePlayBLL = provider.GetRequiredService<ApiSimplePlayBLL>();
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

            foreach (var item in reportData)
            {
                dailyData.Add(new FincanceDailySummary()
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = reportDate,
                    TpId = tpId,
                    CurrencyId = item.CurrencyId,
                    Stake = item.TotalBet,
                    ValidBet = item.TotalBet,
                    Payout = item.TotalBet + item.Winlose,
                    WinlostAmount = item.Winlose
                });
            }
            return dailyData;
        }

        public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            var reportData = new List<dynamic>();
            CurrencyStats_SimplePlay getWagerSummary = await _apiSimplePlayBLL.GetBetSummaryByTime(setting, reportTime.startTime, reportTime.endTime);

            // error condition
            var errorCode = getWagerSummary.GetTransactionDetailsResponse?.ErrorMsgId ?? getWagerSummary.APIResponse.ErrorMsgId;
            if (errorCode != 0)
            {
                var msg = "THIRD_PARTY_ERROR";
                _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                SystemException systemException = new SystemException($"{msg} - GetReport({tpId})");
                throw systemException;
            }
            if (errorCode == 0 && getWagerSummary.GetTransactionDetailsResponse.Count == 0)
            {
                var msg = "NO_DATA";
                _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                return reportData;
            }

            reportData.Add(getWagerSummary.GetTransactionDetailsResponse);
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
