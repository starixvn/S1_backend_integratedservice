using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataLC_SA
    {
        public string username { get; set; }
        public string currency { get; set; }
        public decimal amount { get; set; }
        public DateTime Payouttime { get; set; }
        public string txnid { get; set; }
        public string gametype { get; set; }
        public string txn_reverse_id { get; set; }
        public DateTime timestamp { get; set; }
        public string gameid { get; set; }
        public string hostid { get; set; }
        public string Betdetails { get; set; }
        public TicketStatus TicketStatus { get; set; }
    }
    public class PayoutDataLC_SA
    {
        public string username { get; set; }
        public string currency { get; set; }
        public decimal amount { get; set; }
        public long txnid { get; set; }
        public DateTime timestamp { get; set; }
        public string gametype { get; set; }
        public DateTime Payouttime { get; set; }
        public string gameid { get; set; }
        public int hostid { get; set; }
    }

    public class SettleData_SA : SettleData
    {
        public new string Gameid { get; set; }
    }
}
