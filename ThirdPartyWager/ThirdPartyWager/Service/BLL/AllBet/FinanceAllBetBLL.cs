using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class FinanceAllBetBLL : ITpReportBLL
    {
        private readonly ILogger<FinanceAllBetBLL> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;
        private readonly ApiAllBetBLL _apiAllBetBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _timeZone = 8;

        public FinanceAllBetBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceAllBetBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiAllBetBLL = provider.GetRequiredService<ApiAllBetBLL>();
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
                    Stake = item.totalBetAmount,
                    ValidBet = item.totalBetAmount,
                    Payout = item.totalBetAmount + item.totalWinOrLoss,
                    WinlostAmount = item.totalWinOrLoss

                });
            }

            return dailyData;
        }

        public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            var tpCurrencyList = _commonBLL.GetTpCurrencyList(tpId);
            var reportData = new List<dynamic>();

            foreach (var item in tpCurrencyList)
            {
                var currency = item.TpCurrencyId;
                var agent = setting[$"{currency}Agent"];
                var getWagerSummary = await _apiAllBetBLL.GetBetSummaryByTime(setting, agent, reportTime.startTime);
                Thread.Sleep(150000);

                if (getWagerSummary == null)
                {
                    var msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{currency}\r\nagent={agent}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    SystemException systemException = new SystemException($"{msg} - GetReport({tpId})");
                    throw systemException;
                }

                var errorCode = getWagerSummary.resultCode;
                if (errorCode != "OK")
                {
                    var msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{currency}\r\nagent={agent}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    SystemException systemException = new SystemException($"{msg} - GetReport({tpId})");
                    throw systemException;
                }

                if (getWagerSummary.data.count == 0)
                {
                    var msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\nagent={agent}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    continue;
                }

                getWagerSummary.data.CurrencyId = item.CurrencyId;
                reportData.Add(getWagerSummary.data);
            }

            return reportData;
        }

        public TimeCast GetReportTime(DateTime startTime, int day)
        {
            var reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd", startTime);
            var cvTime = DateTimeOffset.Now.AddDays(day).ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (reportTime.endUnix > cvTime)
            {
                reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd", startTime.AddDays(-1));
            }
            return reportTime;
        }
    }
}
