namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingData_Skywind
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
        public Decimal amount { get; set; }
    }

    public class SettleDataRNG_Skywind
    {
        public int SQ { get; set; }
        public string merchid { get; set; }
        public string merchpwd { get; set; }
        public string custid { get; set; }
        public string custsessionid { get; set; }
        public string roundid { get; set; }
        public decimal amount { get; set; }
        public string currencycode { get; set; }
        public string gamecode { get; set; }
        public string trxid { get; set; }
        public long gameid { get; set; }
        public string eventtype { get; set; } = "";
        public int eventid { get; set; }
        public long timestamp { get; set; }
        public string gametype { get; set; } = "";
        public string platform { get; set; } = "";
        public string promoid { get; set; } = "";
        public string promopid { get; set; } = "";
        public decimal jpcontribution { get; set; } = 0;
        public long operationts { get; set; }
        public string distributiontype { get; set; } = "";
        public int TicketStatus { get; set; }
        public int TicketType { get; set; }
        public int jpwin { get; set; } = 0;
        public string gamestatus { get; set; } = "";
        public string jpids { get; set; } = null;
    }
    public class CancelBetData_Skywind
    {
        public string merchid { get; set; }
        public string merchpwd { get; set; }
        public string custid { get; set; }
        public string custsessionid { get; set; }
        public string roundid { get; set; } = "";
        public string currencycode { get; set; } = "";
        public string gamecode { get; set; } = "";
        public string trxid { get; set; } = "";
        public long gameid { get; set; }
        public string eventtype { get; set; } = "";
        public long timestamp { get; set; }
        public long operationts { get; set; }
    }
    public class SettleData_Skywind : SettleData
    {
        public decimal debit { get; set; }
        public decimal credit { get; set; }
        public string playerCode { get; set; }
    }
}
