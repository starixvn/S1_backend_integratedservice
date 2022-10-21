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

    public class BetList_AdvantPlayMini
    {
        public BetDetail_AdvantPlayMini result { get; set; }
        public int httpStatus { get; set; }
        public string errorMesssage { get; set; }

    }

    public class BetDetail_AdvantPlayMini
    {
        public int page { get; set; }
        public int totalPage { get; set; }
        public int totalCount { get; set; }
        public IList<Bet_AdvantPlayMini> data { get; set; }
    }

    public class Bet_AdvantPlayMini
    {
        public DateTime transactionDate { get; set; }
        public string gameId { get; set; }
        public string sessionId { get; set; }
        public string userReferenceId { get; set; }
        public string transactionId { get; set; }
        public string transactionReferenceId { get; set; }
        public decimal stake { get; set; }
        public decimal won { get; set; }
        public decimal odd { get; set; }
        public decimal autoCashOut { get; set; }
        public string result { get; set; }
        public DateTime? sessionEndTime { get; set; }
        public DateTime? refundTime { get; set; }
        public string status { get; set; }
        public string currency { get; set; }
        public long transactionUnix
        {
            get
            {
                return new DateTimeOffset(transactionDate, new TimeSpan(0, 0, 0)).ToUnixTimeSeconds();
            }
        }
        public long sessionEndUnix
        {
            get
            {
                return sessionEndTime != null ? new DateTimeOffset((DateTime)sessionEndTime, new TimeSpan(0, 0, 0)).ToUnixTimeSeconds() : 0;
            }
        }
        public long refundUnix
        {
            get
            {
                return refundTime != null ? new DateTimeOffset((DateTime)refundTime, new TimeSpan(0, 0, 0)).ToUnixTimeSeconds() : 0;
            }
        }
    }

    public class CurrencyStats_AdvantPlayMini
    {
        public int httpStatus { get; set; }
        public string errorMesssage { get; set; }
        public IList<CurrencyStatsData_AdvantPlayMini> result { get; set; }
    }

    public class CurrencyStatsData_AdvantPlayMini
    {
        public decimal totalStake { get; set; }
        public decimal totalWin { get; set; }
        public decimal totalRefund { get; set; }
        public string currency { get; set; }
        public string gameId { get; set; }
    }
}
