using System.Text.Json.Serialization;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_BNG
    {
        public decimal bet { get; set; }
        public decimal win { get; set; }
        public string currency { get; set; }
        public string id { get; set; }
        public bool round_started { get; set; }
        public bool round_finished { get; set; }
    }
    public class BNGWager
    {
        public decimal bet { get; set; }
        public decimal win { get; set; }
        public long c_at { get; set; }
        public string brand { get; set; }
        public bool round_finished { get; set; }
        public string game_id { get; set; }
    }

    public class BNGAuthenticateResponse
    {
        public string uid { get; set; }
        public player player { get; set; }
        public balance balance { get; set; }
        public string tag { get; set; }
    }
    public class BNGResponse
    {
        public string uid { get; set; }
        public player player { get; set; }
        public balance balance { get; set; }
        public string tag { get; set; }
        public error error { get; set; }
    }
    public class BNGTransactionResponse
    {
        public string uid { get; set; }
        public balance balance { get; set; }
    }
    public class BNGTransactionErrorResponse : BNGTransactionResponse
    {
        public error error { get; set; }
    }
    public class player
    {
        public string id { get; set; }
        public string brand { get; set; }
        public string currency { get; set; }
        public string mode { get; set; }
        public bool is_test { get; set; }
    }
    public class balance
    {
        public string value { get; set; }
        public long version { get; set; }
    }
    public class error
    {
        public string code { get; set; }
    }
    public class BNGRequest
    {
        public string name { get; set; }
        public string uid { get; set; }
        public string token { get; set; }
        public string session { get; set; }
        public string game_id { get; set; }
        public string game_name { get; set; }
        public string provider_id { get; set; }
        public string provider_name { get; set; }
        public string c_at { get; set; }
        public string sent_at { get; set; }
        public args args { get; set; }
    }
    public class args
    {
        public string platform { get; set; }
        public string bet { get; set; }
        public string win { get; set; }
        public bool round_started { get; set; }
        public bool round_finished { get; set; }
        public long round_id { get; set; }
        public bonus bonus { get; set; }
        public player player { get; set; }
        public string tag { get; set; }
        public string transaction_uid { get; set; }
    }
    public class bonus
    {
        public string campaign { get; set; }
        public string source { get; set; }
        public int bonus_id { get; set; }
        public string ext_bonus_id { get; set; }
        public string bonus_type { get; set; }
        [JsonPropertyName("event")]
        public string Event { get; set; }
        public string start_date { get; set; }
        public string end_date { get; set; }
        public string total_bet { get; set; }
        public string total_win { get; set; }
        public string played_bet { get; set; }
        public string played_win { get; set; }
        public string status { get; set; }
    }


    public class BettransRng_BNG
    {
        public string uid { get; set; }
        public string token { get; set; }
        public string session { get; set; }
        public string game_id { get; set; }
        public string game_name { get; set; }
        public string provider_id { get; set; }
        public string provider_name { get; set; }
        public DateTime c_at { get; set; }
        public DateTime sent_at { get; set; }
        public string platform { get; set; }
        public decimal bet { get; set; }
        public decimal win { get; set; }
        public bool round_started { get; set; }
        public bool round_finished { get; set; }
        public long round_id { get; set; }
        public string id { get; set; }
        public string brand { get; set; }
        public string currency { get; set; }
        public string mode { get; set; }
        public bool is_test { get; set; }
        public string bonus { get; set; }
        public string tag { get; set; }
        public int TicketType { get; set; }
    }
    public enum ErrorCode
    {
        INVALID_TOKEN,
        EXPIRED_TOKEN,
        GAME_NOT_ALLOWED,
        TIME_EXCEED,
        LOSS_EXCEED,
        BET_EXCEED,
        FUNDS_EXCEED,
        OTHER_EXCEED,
        SESSION_CLOSED
    }
    public enum ActionName
    {
        login,
        transaction,
        rollback,
        getbalance
    }

    public enum TransactionStatus
    {
        Success = 0,
        Fail = 1
    }
    //public class Result
    //{
    //    public ResultCode Code { get; set; }
    //    public string Message { get; set; }
    //}
}
