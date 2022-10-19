namespace ThirdPartyWager.Service.Model
{
    public class WagerSimplePlayModel
    {
        public GetAllBetDetailsResponse_SimplePlay GetAllBetDetailsForTimeIntervalResponse { get; set; }
        public APIResponse APIResponse { get; set; }
    }

    public class GetAllBetDetailsResponse_SimplePlay
    {
        public int ErrorMsgId { get; set; }
        public string ErrorMsg { get; set; }
        public BetDetailList_SimplePlay BetDetailList { get; set; }
    }

    public class BetDetailList_SimplePlay
    {
        public IList<Bet_SimplePlay> BetDetail { get; set; }
    }

    public class Bet_SimplePlay
    {
        public DateTime BetTime { get; set; }
        public DateTime PayoutTime { get; set; }
        public string Username { get; set; }
        public string GameID { get; set; }
        public long BetID { get; set; }
        public decimal BetAmount { get; set; }
        public decimal Rolling { get; set; }
        public decimal ResultAmount { get; set; }
        public decimal Balance { get; set; }
        public string GameType { get; set; }
        public int BetSource { get; set; }
        public string Detail { get; set; }
        public long TransactionID { get; set; }
        public decimal JackpotContribution { get; set; }
        public int PrizeID { get; set; }
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

    public class CurrencyStats_SimplePlay
    {
        public CurrencyStatsData_SimplePlay GetTransactionDetailsResponse { get; set; }
        public APIResponse APIResponse { get; set; }
    }

    public class CurrencyStatsData_SimplePlay
    {
        public int ErrorMsgId { get; set; }
        public string ErrorMsg { get; set; }
        public int Count { get; set; }
        public decimal Winlose { get; set; }
        public decimal TotalBet { get; set; }
        public int CurrencyId { get; set; }
    }

}
