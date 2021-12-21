using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingData_AP
    {
        public string seq { get; set; }
        public string sitecode { get; set; }
        public string gamecode { get; set; }
        public string gameroundid { get; set; }
        public decimal stake { get; set; }
        public string bettime { get; set; }
        public bool ispromo { get; set; }
        public string ip { get; set; }
        public string optoken { get; set; }
        public string timestamp { get; set; }
        public string playerid { get; set; }
        public int TicketStatus { get; set; }
        public int TicketType { get; set; }
    }
    public class CancelBetDataRNG_AP
    {
        public string seq { get; set; }
        public string sitecode { get; set; }
        public string gamecode { get; set; }
        public string gameroundid { get; set; }
        public string timestamp { get; set; }
        public string playerid { get; set; }
        public decimal totalstake { get; set; }
    }
    public class BettransRNG_AP 
    { 
        public int SQ { get; set; }
    }

}
