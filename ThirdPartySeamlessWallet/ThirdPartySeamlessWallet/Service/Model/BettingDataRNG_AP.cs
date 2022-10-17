using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_AP
    {
        public string seq { get; set; }
        public string sitecode { get; set; }
        public string gamecode { get; set; } = null;
        public string gameroundid { get; set; }
        public decimal stake { get; set; } = 0;
        public decimal totalstake { get; set; } = 0;
        public decimal totalwin { get; set; } = 0;
        public decimal jpwin { get; set; } = 0;
        public string bettime { get; set; } = null;
        public string settletime { get; set; } = null;
        public string jpsettletime { get; set; } = null;
        public string ip { get; set; } = null;
        public string timestamp { get; set; }
        public string playerid { get; set; }
        public int hitjackpot { get; set; } = 0;
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
        public long SQ { get; set; }
        public int TicketStatus { get; set; }
    }
    public class SettleDataRNG_AP 
    {
        public string seq { get; set; }
        public string sitecode { get; set; }
        public string gamecode { get; set; }
        public string gameroundid { get; set; }
        public decimal stake { get; set; } = 0;
        public decimal totalstake { get; set; } = 0;
        public decimal totalwin { get; set; } = 0;
        public decimal jpwin { get; set; } = 0;
        public long bettime { get; set; }
        public long settletime { get; set; }
        public long jpsettletime { get; set; }
        public string ip { get; set; } = null;
        public long timestamp { get; set; }
        public string playerid { get; set; }
        public int hitjackpot { get; set; } = 0;
        public int TicketStatus { get; set; }
        public int TicketType { get; set; }
    }
    public class CPSettleDataRNG_AP
    {
        public string seq { get; set; }
        public string sitecode { get; set; }
        public string playerid { get; set; }
        public string cpid { get; set; }
        public string cptransid { get; set; }
        public DateTime timestamp { get; set; }
        public decimal cpwin { get; set; }
        public long timeunix
        {
            get
            {
                return new DateTimeOffset(timestamp).ToUnixTimeSeconds();
            }
        }

    }

}
