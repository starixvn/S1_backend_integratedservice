using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_Joker
    {
        public decimal amount { get; set; }
        public string appid { get; set; }
        public string gamecode { get; set; }
        public string id { get; set; }
        public long timestamp { get; set; }
        public string username { get; set; }
        public string roundid { get; set; }
        public string type { get; set; }
        public int TicketStatus { get; set; }
    }
    public class CancelBetDataRNG_Joker
    {
        public string appid { get; set; }
        public string gamecode { get; set; }
        public string id { get; set; }
        public string betid { get; set; }
        public long timestamp { get; set; }
        public string username { get; set; }
        public string roundid { get; set; }
        public string type { get; set; }
        public int TicketStatus { get; set; }
    }
}
