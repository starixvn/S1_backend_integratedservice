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

    public class SettleDataRNG_GPI : SettleData
    {
        public string cust_id { get; set; }
        public string round_id { get; set; }
        public string game_id { get; set; }
        public int status { get; set; }
    }

    public class CancelBet_GPI
    {
        public string cust_id { get; set; }
        public string Operator { get; set; }
        public string trx_id { get; set; }
        public string game_id { get; set; }
        public string round_id { get; set; }
        public string currency { get; set; }
    }

    public class BettransData_GPI : BettingDataRNG_GPI
    {
        public int Edit { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
    }

}
