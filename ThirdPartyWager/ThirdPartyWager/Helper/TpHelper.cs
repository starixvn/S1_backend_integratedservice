using Newtonsoft.Json;

using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;


namespace ThirdPartyWager.Helper
{
    public static class TpHelper
    {
        /// <summary>
        /// When vendor pass tpCurrencyId not exists on my system, we will use default id=4 (currency: TBH)
        /// </summary>
        private const int DEFAULT_CURRENCYID = 4;

        private static int GetCurrencyId(IDictionary<string, CurrencyModel> currency, string tpCurrency)
        {
            return currency.ContainsKey(tpCurrency) ? currency[tpCurrency].CurrencyId : DEFAULT_CURRENCYID;
        }
        public static IEnumerable<FincanceDailySummary> DealReport(
            IDictionary<string, CurrencyModel> currency,
            int tpId, 
            DateTime reportDate,
            List<dynamic> reportData)
        {
            var getReportData =
                JsonConvert.DeserializeObject<List<PayloadDailySummaryData_MaxGame>>(
                    JsonConvert.SerializeObject(reportData));
            
            return getReportData
                .Select(i => i.GameTypes)
                .SelectMany(x => x)
                .Select(i => i.BettingDate)
                .SelectMany(x => x)
                .GroupBy(i => i.CurrencyIso)
            .Select(g => new
                {
                    CurrencyIso=g.Key,
                    CurrencyId= GetCurrencyId(currency, g.Key),
                    Stake = g.Sum(s => s.Stake),
                    ActualStake = g.Sum(s => s.ActualStake),
                    NetStake = g.Sum(s => s.NetStake),
                    WinLose = g.Sum(s => s.WinLose),
                    JackpotContributionBetCount = g.Sum(s => s.JackpotContributionBetCount),
                    JackpotContribution = g.Sum(s => s.JackpotContribution),
                    JackpotPrizeBetCount = g.Sum(s => s.JackpotPrizeBetCount),
                    JackpotPrize = g.Sum(s => s.JackpotPrize),
                    PromotionPrizeBetCount = g.Sum(s => s.PromotionPrizeBetCount),
                    PromotionPrize = g.Sum(s => s.PromotionPrize)
                }).Select(item => new FincanceDailySummary
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = reportDate,
                    TpId = tpId,
                    CurrencyId = item.CurrencyId,
                    CurrencyIso = item.CurrencyIso,
                    Stake = item.Stake,
                    ValidBet = item.NetStake,
                    WinlostAmount = (item.WinLose - item.NetStake),
                    Payout = item.WinLose
                });
        }
    }
}
