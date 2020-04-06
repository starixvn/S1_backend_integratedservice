using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_YGG
    {
        public string playerid { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public string reference { get; set; }
        public string subreference { get; set; }
        public int TicketStatus { get; set; }
    }

    public class SettledDataRNG_YGG
    {
        public string playerid { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public string reference { get; set; }
        public string subreference { get; set; }
        public int tickets { get; set; }
        public int TicketStatus { get; set; }
    }
}
