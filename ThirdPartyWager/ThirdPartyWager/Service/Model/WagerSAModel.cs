namespace ThirdPartyWager.Service.Model
{
    public class WagerSAModel
    {
        public GetAllBetDetailsForTimeIntervalResponse GetAllBetDetailsForTimeIntervalResponse { get; set; }
        public APIResponse APIResponse { get; set; }
    }

    public class GetAllBetDetailsForTimeIntervalResponse
    {
        public int ErrorMsgId { get; set; }
        public string ErrorMsg { get; set; }
        public BetDetailList BetDetailList { get; set; }
    }

    public class BetDetailList
    {
        public IList<BetSA> BetDetail { get; set; }
    }

    public class BetSA
    {
        public DateTime BetTime { get; set; }
        public DateTime PayoutTime { get; set; }
        public string Username { get; set; }
        public int HostID { get; set; }
        public string GameID { get; set; }
        public int Round { get; set; }
        public int Set { get; set; }
        public long BetID { get; set; }
        public decimal BetAmount { get; set; }
        public decimal Rolling { get; set; }
        public decimal ResultAmount { get; set; }
        public decimal Balance { get; set; }
        public string GameType { get; set; }
        public int BetType { get; set; }
        public int BetSource { get; set; }
        public string Detail { get; set; }
        public long TransactionID { get; set; }
        public dynamic GameResult { get; set; }
        public bool State { get; set; }
        public long betUnix
        {
            get
            {
                return new DateTimeOffset(BetTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
        public long payOutUnix
        {
            get
            {
                return new DateTimeOffset(PayoutTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
    }

    public class APIResponse
    {
        public int ErrorMsgId { get; set; }
        public string ErrorMsg { get; set; }
    }

    public class WagerRedoSAModel
    {
        public GetTransactionDetailsResponse GetTransactionDetailsResponse { get; set; }
        public APIResponse APIResponse { get; set; }
    }

    public class GetTransactionDetailsResponse
    {
        public int ErrorMsgId { get; set; }
        public string ErrorMsg { get; set; }
        public Details Details { get; set; }
    }

    public class Details
    {
        public IList<TransactionDetails> TransactionDetails { get; set; }
    }

    public class TransactionDetails
    {
        public string CurrencyName { get; set; }
        public int Count { get; set; }
        public decimal Winlose { get; set; }
        public decimal TotalBet { get; set; }
        public decimal TotalRolling { get; set; }
    }

    public class WagerSATotal
    {
        public int WagerCount { get; set; }
        public int SiteId { get; set; }
        public int MainSiteId { get; set; }
        public int TpCurrencyId { get; set; }
    }

    public class CurrencyStatistics_SA
    {
        public int MainSiteId { get; set; }
        public int WagerCount { get; set; }

    }
}
