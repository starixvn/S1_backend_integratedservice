namespace ThirdPartyWager.Service.Model
{
    public class CurrencyStats_AP
    {
        public DateTime Timestamp { get; set; }
        public string Seq { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorDescription { get; set; }
        public IList<CurrencyStatsData_AP> Results { get; set; }
    }

    public class CurrencyStatsData_AP
    {
        public string SiteCode { get; set; }
        public string Currency { get; set; }
        public int TicketCount { get; set; }
        public decimal TotalStake { get; set; }
        public decimal TotalWin { get; set; }
    }
}
