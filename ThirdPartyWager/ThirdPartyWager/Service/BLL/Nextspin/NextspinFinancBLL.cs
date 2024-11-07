using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public interface INextspinFinancBLL
    {
        Task ExecReportAsync(int tpId, int tpType, TimeCast reportTime);
    }

    public class NextspinFinancBLL : INextspinFinancBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly INextspinApi _apiNextspinBll;
        private readonly CommonBLL _commonBLL;
        private readonly FincanceService _fincanceService;
        private readonly IEnumerable<TpCurrency> _tpCurrencyList;
        private readonly ILogger<NextspinFinancBLL> _logger;
        private readonly Dictionary<int, NextspinConfig> _siteConfigDic;
        private readonly int _tpId = (int)Tp.Netent;

        public NextspinFinancBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiNextspinBll = provider.GetRequiredService<INextspinApi>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _fincanceService = provider.GetRequiredService<FincanceService>();
            _tpCurrencyList = _commonBLL.GetTpCurrencyList((int)Tp.Nextspin);
            _logger = provider.GetRequiredService<ILogger<NextspinFinancBLL>>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<NextspinConfig>(Tp.Nextspin.ToString());
        }

        public async Task ExecReportAsync(int tpId, int tpType, TimeCast reportTime)
        {
            var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
            var reportList = new List<NextspinProfitLossInfo>();
            foreach (var site in siteList)
            {
                if (site.TicketKey == 0)
                {
                    continue;
                }

                var config = _siteConfigDic[site.SiteId];
                var profitLoss = await _apiNextspinBll.RetrieveProfitLoss(reportTime.startTime, reportTime.endTime, 1, config);
                if (profitLoss != null && profitLoss.List != null && profitLoss.List.Count > 0)
                {
                    reportList.AddRange(profitLoss.List);
                }
            }

            var reportDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(reportTime.startUnix);
            // process
            var dailySummary = DealReport(tpId, reportDate, reportList);

            // write
            _fincanceService.SetFinanceTpDailySummary(dailySummary);

            _thirdPartyBLL.SetThirdPartyTimer(tpId, tpType, reportTime.endUnix);
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
