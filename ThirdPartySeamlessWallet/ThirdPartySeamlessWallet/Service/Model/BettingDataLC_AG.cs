using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class DataLC_AG
    {
        public string SessionToken { get; set; }
        public string TransactionType { get; set; }
        public string TransactionCode { get; set; }
        public string TransactionID { get; set; }
        public string Gametype { get; set; }
        public string GameCode { get; set; }
        public string TableCode { get; set; }
        public string Playname { get; set; }
        public string AgentCode { get; set; }
        public string Currency { get; set; }
    }
    public class BettingDataLC_AG : DataLC_AG
    {
        public string DeviceType { get; set; }
        public string Playtype { get; set; }
        public string PlatformType { get; set; }
        public string Round { get; set; }
        public decimal Value { get; set; }
        public DateTime BetTime { get; set; }
    }

    public class PayoutDataLC_AG : DataLC_AG
    {
        public List<string> TransactionIDs { get; set; }
        public decimal NetAmount { get; set; }
        public decimal ValidBetAmount { get; set; }
        public DateTime Settletime { get; set; }
        public string BillNo { get; set; }
        public string TicketStatus { get; set; }
        public string GameResult { get; set; }
        public string Finish { get; set; }
        public string Memo { get; set; }
    }
    public class SettleDataLC_AG : DataLC_AG
    {
        public string TransactionIDs { get; set; }
        public decimal NetAmount { get; set; }
        public decimal ValidBetAmount { get; set; }
        public DateTime Settletime { get; set; }
        public string BillNo { get; set; }
        public string TicketStatus { get; set; }
        public string GameResult { get; set; }
        public string Finish { get; set; }
        public string Memo { get; set; }
    }
    public class BettingDataLC_AG_Refund : BettingDataLC_AG
    {
        public string BillNo { get; set; }
    }
}
