namespace ThirdPartyWager.Service.Model
{
    public class CurrencyStats_Sexy
    {
        public string status { get; set; }
        public string desc { get; set; }
        public IList<CurrencyStatsData_Sexy> transactions { get; set; }
    }

    public class CurrencyStatsData_Sexy
    {
        public string gameType { get; set; }
        public string gameName { get; set; }
        public string gameCode { get; set; }
        public string currency { get; set; }
        public string platform { get; set; }
        public decimal turnover { get; set; }
        public int betCount { get; set; }
        public decimal betAmount { get; set; }
        public decimal realBetAmount { get; set; }
        public decimal winAmount { get; set; }
        public decimal jackpotBetAmount { get; set; }
        public decimal jackpotWinAmount { get; set; }
        public decimal realWinAmount { get; set; }
    }

}
