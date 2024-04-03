using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class FinanceAGBLL : ITpReportBLL
    {
        private readonly ILogger<FinanceAGBLL> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;
        private readonly ApiAGBLL _apiAGBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _timeZone = -4;
        private readonly AGConfig _siteAgConfig;

        public FinanceAGBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceAGBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiAGBLL = provider.GetRequiredService<ApiAGBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _siteAgConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<AGConfig>(Tp.AG.ToString());
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
                    CurrencyId = _currencyId[item.currency],
                    Stake = item.betAmount,
                    ValidBet = item.validBetAmount,
                    Payout = item.betAmount + item.netAmount,
                    WinlostAmount = item.netAmount

                });
            }

            return dailyData;
        }

        public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            var guid = Guid.NewGuid().ToString();
            var reportData = new List<dynamic>();

            var getWagerSummary = await _apiAGBLL.GetBetSummaryByTime(_siteAgConfig.WagerUrl, _siteAgConfig.HashKey, _siteAgConfig.data_cagent, reportTime.startTime, reportTime.endTime, guid);

            // error condition
            var arrSummary = getWagerSummary.result;
            var errorCode = arrSummary.info;
            if (errorCode != 0)
            {
                var msg = "THIRD_PARTY_ERROR";
                _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                SystemException systemException = new SystemException($"{msg} - GetReport({tpId})");
                throw systemException;
            }

            if (errorCode == 0 && arrSummary.row == null)
            {
                var msg = "NO_DATA";
                _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                return reportData;
            }

            foreach (var item in arrSummary.row)
            {
                reportData.Add(item);
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
