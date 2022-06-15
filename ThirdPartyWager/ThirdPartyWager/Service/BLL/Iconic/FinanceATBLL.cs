using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class FinanceATBLL : ITpReportBLL
    {
        private readonly ILogger<FinanceATBLL> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;
        private readonly ApiIconicBLL _apiIconicBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _timeZone = 0;

        public FinanceATBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceATBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiIconicBLL = provider.GetRequiredService<ApiIconicBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        }

        private readonly Dictionary<int, int> _currencyIdList = new Dictionary<int, int>() {
            {050,74},// BDT
            {156,13},// CNY
            {356,61},// INR
            {458,2},// MYR
            {764,4},// THB
            {840,3},// USD
            {1000,51},// VND1000
            {1001,15},// IDR1000
        };

        public IEnumerable<FincanceDailySummary> DealReport(int tpId, DateTime reportDate, List<dynamic> reportData)
        {
            var dailyData = new List<FincanceDailySummary>();
            if (!reportData.Any())
            {
                return dailyData;
            }

            foreach (var item in reportData)
            {
                dailyData.Add(new FincanceDailySummary()
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = reportDate,
                    TpId = tpId,
                    CurrencyId = _currencyIdList[item.currency],
                    Stake = item.bet,
                    ValidBet = item.validBet,
                    Payout = item.win,
                    WinlostAmount = item.profit

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

                var getWagerSummary = await _apiIconicBLL.GetBetSummaryByTime(setting["ApiUrl"], setting[$"token_{currency}"], reportTime.startUnix, reportTime.endUnix);
                if (getWagerSummary.error != null)
                {
                    var errorCode = getWagerSummary.error.status;
                    if (errorCode != 0)
                    {
                        var msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\n{currency}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                        SystemException systemException = new SystemException($"{msg} - GetReport({tpId})");
                        throw systemException;
                    }
                }

                if (getWagerSummary.data.Count == 0)
                {
                    var msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    continue;
                }

                reportData.AddRange(getWagerSummary.data);
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
