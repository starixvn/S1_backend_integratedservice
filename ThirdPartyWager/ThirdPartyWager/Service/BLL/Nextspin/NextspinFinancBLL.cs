using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.DAL;

namespace ThirdPartyWager.Service.BLL.Nextspin
{
    public interface INextspinFinancBLL
    {
        Task<int> ExecReportAsync(int tpId, TimeCast reportTime, DateTime reportDate);
    }

    public class NextspinFinancBLL : INextspinFinancBLL
    {
        private readonly CommonBLL _commonBLL;
        private readonly FincanceService _fincanceService;
        private readonly IEnumerable<TpCurrency> _tpCurrencyList;
        private readonly ILogger<NextspinFinancBLL> _logger;


        public NextspinFinancBLL(CommonBLL commonBLL, FincanceService fincanceService, ILogger<NextspinFinancBLL> logger)
        {
            _commonBLL = commonBLL;
            _fincanceService = fincanceService;
            _tpCurrencyList = _commonBLL.GetTpCurrencyList((int)Tp.Nextspin);
            _logger = logger;
        }

        public async Task<int> ExecReportAsync(int tpId, TimeCast reportTime, DateTime reportDate)
        {
            var reportData = await GetReport(reportTime);
            if (reportData.Count > 0)
            {
                // process
                var dailyData = DealReport(tpId, reportDate, reportData);

                // write
                return _fincanceService.SetFinanceTpDailySummary(dailyData);
            }
            return 1;
        }

        private IEnumerable<FincanceDailySummary> DealReport(int tpId, DateTime reportDate, List<dynamic> reportData)
        {
            var dailyData = new List<FincanceDailySummary>();
            if (!reportData.Any())
            {
                return dailyData;
            }

            var _currencyId = _tpCurrencyList.ToDictionary(i => i.TpCurrencyId, i => i.CurrencyId);

            var getReportData = JsonConvert.DeserializeObject<List<FincanceDailySummary>>(JsonConvert.SerializeObject(reportData));
            foreach (var item in getReportData)
            {
                dailyData.Add(new FincanceDailySummary()
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = reportDate,
                    TpId = tpId,
                    CurrencyId = _currencyId[item.TpCurrencyId],
                    Stake = item.Stake,
                    ValidBet = item.ValidBet,
                    Payout = item.Payout,
                    WinlostAmount = item.WinlostAmount
                });
            }

            return dailyData;
        }

        private async Task<List<dynamic>> GetReport(TimeCast reportTime)
        {
            var reportData = new List<dynamic>();
            var bettrans = await _commonBLL.GetWagerSummary("Finance_TpWagerRng_Nextspin_Summary_Get", reportTime.startUnix, reportTime.endUnix);
            reportData.AddRange(bettrans);
            return reportData;
        }
    }
}
