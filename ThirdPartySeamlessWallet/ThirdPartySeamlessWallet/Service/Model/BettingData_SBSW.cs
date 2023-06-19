namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingData_SBSW
    {
    }
    public class BetSBSW 
    { 
        public string userId { get; set; }
        public decimal betAmount { get; set; }
        public decimal actualAmount { get; set; }
        public decimal creditAmount { get; set; }
        public decimal debitAmount { get; set; }
        public int TicketType { get; set; }
        public DateTime transactionTime { get; set; }
    }
    public class SettleSBSW
    {
        public string refId { get; set; }
        public decimal payout { get; set; }
        public decimal creditAmount { get; set; }
        public decimal debitAmount { get; set; }
        public int TicketType { get; set; }
    }
}
