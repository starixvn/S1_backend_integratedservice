using Newtonsoft.Json;
using ThirdPartyWager.Extensions;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Helper
{
    public static class TpHelper
    {
        public static IEnumerable<FincanceDailySummary> DealReport(
            IDictionary<string, CurrencyModel> currencyIdDic, 
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
                    currencyIdDic.GetValue(g.Key).CurrencyId,
                    currencyIdDic.GetValue(g.Key).ExchangeRate,
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
                    Stake = item.Stake * item.ExchangeRate,
                    ValidBet = item.Stake * item.ExchangeRate,
                    WinlostAmount = (item.WinLose - item.NetStake) * item.ExchangeRate,
                    Payout = item.WinLose * item.ExchangeRate
                });
        }
    }
}
