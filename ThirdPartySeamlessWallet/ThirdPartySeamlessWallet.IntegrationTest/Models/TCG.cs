using System;
using System.Collections.Generic;
using System.Text;

namespace ThirdPartySeamlessWallet.IntegrationTest.Models
{
    public class TCGRecord
    {
        public string username { get; set; }
        public string ref_no { get; set; }     
        public decimal amount { get; set; }  
        public string TransType { get; set; }
        //public int TicketType { get; set; }
        //public int TicketStatus { get; set; }
        //public int SettleFlag { get; set; }
    }
}
