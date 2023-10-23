namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_BNG
    {
    }
    public class BNGResponse
    {
        public string uid { get; set; }
        public player player { get; set; }
        public balance balance { get; set; }
        public string tag { get; set; }
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
        public int version { get; set; }
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
    }
    public class error
    {
        public string code { get; set; }
    }
    public enum ErrorCode
    {
        INVALID_TOKEN,
        EXPIRED_TOKEN,
        GAME_NOT_ALLOWED
    }
    public enum ActionName
    {
        login,
        transaction,
        rollback,
        getbalance
    }
}
