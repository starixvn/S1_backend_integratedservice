using System;
using System.Collections.Generic;
using System.Text;

namespace ThirdPartySeamlessWallet.IntegrationTest.Models
{
    public class ReserveRecord
    {
        public string cust_id { get; set; }
        public string reserve_id { get; set; }
        public decimal amount { get; set; }
        public short TicketType { get; set; }
        public short TicketStatus { get; set; }
        public short SettleFlag { get; set; }
    }
}
