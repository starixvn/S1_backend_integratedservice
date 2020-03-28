using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_GPI
    {
        public string cust_id { get; set; }
        public string currency { get; set; }
        public decimal amount { get; set; }
        public string trx_id { get; set; }
        public string Operator { get; set; }
        public string game_id { get; set; }
        public string round_id { get; set; }
        public string bundle_id { get; set; }
        public decimal? jackpot_contribution { get; set; }
        public string platform { get; set; }
    }

    public class SettledDataRNG_GPI
    {
        public string cust_id { get; set; }
        public string currency { get; set; }
        public decimal amount { get; set; }
        public string trx_id { get; set; }
        public string Operator { get; set; }
        public string game_id { get; set; }
        public string round_id { get; set; }
        public string bundle_id { get; set; }
        public decimal? jackpot_win { get; set; }
        public string platform { get; set; }
        public decimal? rake { get; set; }
        public decimal? rakedhands { get; set; }
    }
    public class SettledDataRNG_GPIforGet : SettledDataRNG_GPI
    {
        public string TransId { get; set; }
        public TicketStatus TicketStatus { get; set; }
    }
    public class EndgameRNG_GPI
    {
        public string cust_id { get; set; }
        public string Operator { get; set; }
        public string game_id { get; set; }
        public string round_id { get; set; }
    }
}
