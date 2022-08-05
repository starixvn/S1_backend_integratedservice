namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_Skywind
    {
        public string merch_id { get; set; }
        public string merch_pwd { get; set; }
        public string cust_id { get; set; }
        public string cust_session_id { get; set; }
        public string round_id { get; set; }
        public decimal amount { get; set; }
        public string currency_code { get; set; }
        public string game_code { get; set; }
        public string trx_id { get; set; }
        public string game_id { get; set; }
        public string event_type { get; set; }
        public int event_id { get; set; }
        public int timestamp { get; set; }
        public string geme_type { get; set; }
        public string platform { get; set; }
        public string promo_id { get; set; }
        public decimal jp_contribution { get; set; } = 0;
        public int operation_ts { get; set; }
        public string distribution_type { get; set; }
        public int TicketStatus { get; set; }
        public int TicketType { get; set; }
        public bool jp_win { get; set; } = false;
        public string game_status { get; set; }
        public string[] jp_ids { get; set; }
    }

    public class BettransRng_Skywind
    {
        public long SQ { get; set; }
    }
}
