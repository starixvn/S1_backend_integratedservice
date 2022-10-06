namespace ThirdPartyWager.Service.Model
{
    public class CurrencyStats_YL
    {
        public string status { get; set; }
        public string desc { get; set; }
        public IList<CurrencyStatsData_YL> items { get; set; }
    }

    public class CurrencyStatsData_YL
    {
        public string currency { get; set; }
        public int totalSize { get; set; }
        public IList<CurrencyStatsItem_YL> data { get; set; }
        public decimal totalBetAmount { get; set; }
        public decimal totalWinAmount { get; set; }
        public decimal totalWinLoss { get; set; }
        public decimal totalLossAmount { get; set; }
        public decimal totalTxnAmount { get; set; }
        public decimal totalValidBet { get; set; }
        public DateTime dateTime { get; set; }
    }

    public class CurrencyStatsItem_YL
    {
        public string userId { get; set; }
        public string extension1 { get; set; }
        public decimal betAmount { get; set; }
        public decimal winAmount { get; set; }
        public decimal winLoss { get; set; }
        public decimal lossAmount { get; set; }
        public decimal txnAmount { get; set; }
        public decimal validBet { get; set; }
    }
}
