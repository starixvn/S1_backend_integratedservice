using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public interface INextspinFinancBLL
    {
        Task<int> ExecReportAsync(int tpId, TimeCast reportTime);
    }

    public class NextspinFinancBLL : INextspinFinancBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly INextspinApi _apiNextspinBll;
        private readonly CommonBLL _commonBLL;
        private readonly FincanceService _fincanceService;
        private readonly IEnumerable<TpCurrency> _tpCurrencyList;
        private readonly ILogger<NextspinFinancBLL> _logger;


        public NextspinFinancBLL(ThirdPartyBLL thirdPartyBLL, INextspinApi nextspinApi, CommonBLL commonBLL, FincanceService fincanceService, ILogger<NextspinFinancBLL> logger)
        {
            _thirdPartyBLL = thirdPartyBLL;
            _apiNextspinBll = nextspinApi;
            _commonBLL = commonBLL;
            _fincanceService = fincanceService;
            _tpCurrencyList = _commonBLL.GetTpCurrencyList((int)Tp.Nextspin);
            _logger = logger;
        }

        public async Task<int> ExecReportAsync(int tpId, TimeCast reportTime)
        {
            var setting = _thirdPartyBLL.GetThirdPartyBySite<NextspinSetting>((int)Tp.Nextspin, 1);
            var profitLoss = await _apiNextspinBll.RetrieveProfitLoss(reportTime.startTime, reportTime.endTime, 1, setting);
            if (profitLoss == null)
                return -1;

            if (profitLoss.List != null && profitLoss.List.Count > 0)
            {
                var reportDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(reportTime.startUnix);
                // process
                var dailySummary = DealReport(tpId, reportDate, profitLoss.List);

                // write
                return _fincanceService.SetFinanceTpDailySummary(dailySummary);
            }
            return 1;
        }

        private IEnumerable<FincanceDailySummary> DealReport(int tpId, DateTime reportDate, IList<NextspinProfitLossInfo> reportData)
        {
            var _currencyId = _tpCurrencyList.ToDictionary(i => i.TpCurrencyId, i => i.CurrencyId);

            var dailySummary = reportData.GroupBy(a => a.Currency).Select(a => new FincanceDailySummary()
            {
                SummaryType = SummaryType.TpBetRecord,
                SummaryDate = reportDate,
                TpId = tpId,
                CurrencyId = _currencyId[a.Key],
                Stake = a.Sum(b => b.BetAmount),
                ValidBet = a.Sum(b => b.ValidBetAmount),
                Payout = a.Sum(b => b.TotalWL + b.ValidBetAmount),
                WinlostAmount = a.Sum(b => b.TotalWL)
            });
            return dailySummary;
        }
    }
}
