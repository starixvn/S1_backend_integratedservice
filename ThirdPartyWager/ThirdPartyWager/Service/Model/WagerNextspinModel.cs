namespace ThirdPartyWager.Service.Model
{
    public class NextspinSetting
    {
        public string LaunchUrl { get; set; }
        public string ApiUrl { get; set; }
        public string SecretKey { get; set; }
        public string MerchantCode { get; set; }
        public string Agent { get; set; }
    }
    public class NextspinBetInfo
    {
        public string TicketId { get; set; }
        public string AcctId { get; set; }
        public string CategoryId { get; set; }
        public string GameCode { get; set; }
        public string TicketTime { get; set; }
        public string BetIp { get; set; }
        public double BetAmount { get; set; }
        public double WinLoss { get; set; }
        public string Currency { get; set; }
        public string Result { get; set; }
        public decimal JackpotAmount { get; set; }
        public int LuckyDrawId { get; set; }
        public bool Completed { get; set; }
        public long RoundId { get; set; }
        public long Sequence { get; set; }
        public string Channel { get; set; }
        public decimal Balance { get; set; }
        public decimal JpWin { get; set; }
        public string ReferenceId { get; set; }
    }

    public class NextspinBetHistory
    {
        public IList<NextspinBetInfo> List { get; set; }
        public int ResultCount { get; set; }
        public int PageCount { get; set; }
        public string MerchantCode { get; set; }
        public int Code { get; set; }
        public string Msg { get; set; }
        public string SerialNo { get; set; }
    }

    public class NextspinProfitLossInfo
    {
        public int BetCount { get; set; }
        public decimal JpContributeAmt { get; set; }
        public decimal BonusAmt { get; set; }
        public decimal BonusBearAmt { get; set; }
        public decimal JpAmt { get; set; }
        public string MerchantCode { get; set; }
        public string Currency { get; set; }
        public decimal BetAmount { get; set; }
        public decimal ValidBetAmount { get; set; }
        public decimal TotalWL { get; set; }
        public int JpTotalWL { get; set; }
    }

    public class NextspinProfitLoss
    {
        public IList<NextspinProfitLossInfo> List { get; set; }
        public int ResultCount { get; set; }
        public int PageCount { get; set; }
        public string MerchantCode { get; set; }
        public int Code { get; set; }
        public string Msg { get; set; }
        public string SerialNo { get; set; }
    }
}
