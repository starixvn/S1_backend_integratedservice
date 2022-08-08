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
        public long game_id { get; set; }
        public string event_type { get; set; } = "";
        public int event_id { get; set; }
        public long timestamp { get; set; }
        public string game_type { get; set; } = "";
        public string platform { get; set; } = "";
        public string promo_id { get; set; } = "";
        public string promo_pid { get; set; } = "";
        public decimal jp_contribution { get; set; } = 0;
        public long operation_ts { get; set; }
        public string distribution_type { get; set; } = "";
        public int TicketStatus { get; set; }
        public int TicketType { get; set; }
        public int jp_win { get; set; } = 0;
        public string game_status { get; set; } = "";
        public string jp_ids { get; set; } = null;
    }

    public class BettransRng_Skywind
    {
        public long SQ { get; set; }
        public int TicketStatus { get; set; }
    }
}
