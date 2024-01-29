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

    public enum JokerFishTransType
    {
        Transaction,
        Withdraw,
        Deposit,
    }

    public class BettingDataFish_Joker
    {
        public string appid { get; set; }
        public string hash { get; set; }
        public string id { get; set; }
        public decimal amount { get; set; }
        public decimal result { get; set; }
        public string username { get; set; }
        public long timestamp { get; set; }
        public string gamecode { get; set; } = string.Empty;
        public string roundid { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
        public decimal startbalance { get; set; }
        public decimal endbalance { get; set; }
        public int TicketType { get; set; }
    }

    internal class JokerConfig : TpSiteConfig
    {
        public Dictionary<string, CurrencyDataJoker> CurrencyData { get; set; }
    }

    public class CurrencyDataJoker
    {
        public string AppID { get; set; }
        public string Secret { get; set; }
        public string AppID_Test { get; set; }
        public string Secret_Test { get; set; }
    }
}
