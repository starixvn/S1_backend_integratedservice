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
    public class FinanceSABLL : ITpReportBLL
    {
        private readonly ILogger<FinanceSABLL> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;
        private readonly ApiSABLL _apiSABLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _timeZone = 8;

        public FinanceSABLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceSABLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiSABLL = provider.GetRequiredService<ApiSABLL>();
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
                    CurrencyId = _currencyId[item.CurrencyName],
                    Stake = item.TotalBet,
                    ValidBet = item.TotalRolling,
                    Payout = item.TotalBet + item.Winlose,
                    WinlostAmount = item.Winlose

                });
            }
            return dailyData;
        }

        public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            var reportData = new List<dynamic>();
            var getWagerSummary = await _apiSABLL.GetBetSummaryByTime(setting["WagerUrl"], setting[$"EncrypKey"], setting["MD5Key"], setting["SecretKey"], reportTime.startTime, reportTime.endTime, Guid.NewGuid().ToString());

            // error condition
            var errorCode = getWagerSummary.GetTransactionDetailsResponse?.ErrorMsgId ?? getWagerSummary.APIResponse.ErrorMsgId;
            if (errorCode != 0)
            {
                var msg = "THIRD_PARTY_ERROR";
                _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                SystemException systemException = new SystemException($"{msg} - GetReport({tpId})");
                throw systemException;
            }
            if (errorCode == 0 && getWagerSummary.GetTransactionDetailsResponse.Details == null)
            {
                var msg = "NO_DATA";
                _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                return reportData;
            }

            reportData.AddRange(getWagerSummary.GetTransactionDetailsResponse.Details.TransactionDetails);
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
