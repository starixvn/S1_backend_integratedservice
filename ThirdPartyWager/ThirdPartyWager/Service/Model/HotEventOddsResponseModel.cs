using Newtonsoft.Json;

namespace ThirdPartyWager.Service.Model
{
    public class HotEventOddsResponseModel
    {
        [JsonProperty("error_code")]
        public int ErrorCode { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("Data")]
        public HotEventOddsData Data { get; set; }

    }

    public class AllPrice
    {
        [JsonProperty("ParlayPrice")]
        public double ParlayPrice { get; set; }

        [JsonProperty("MalayPrice")]
        public double MalayPrice { get; set; }

        [JsonProperty("DecimalPrice")]
        public double DecimalPrice { get; set; }

        [JsonProperty("HongKongPrice")]
        public double HongKongPrice { get; set; }

        [JsonProperty("IndoPrice")]
        public double IndoPrice { get; set; }

        [JsonProperty("AmericanPrice")]
        public double AmericanPrice { get; set; }
    }

    public class HotEventOddsData
    {
        [JsonProperty("market_version_key")]
        public long MarketVersionKey { get; set; }

        [JsonProperty("markets")]
        public List<Market> Markets { get; set; }
    }

    public class Market
    {
        [JsonProperty("EventID")]
        public int EventID { get; set; }

        [JsonProperty("BetType")]
        public int BetType { get; set; }

        [JsonProperty("MarketID")]
        public int MarketID { get; set; }

        [JsonProperty("MarketCode")]
        public double MarketCode { get; set; }

        [JsonProperty("ChangeTime")]
        public DateTime ChangeTime { get; set; }

        [JsonProperty("IsLive")]
        public bool IsLive { get; set; }

        [JsonProperty("MarketStatus")]
        public string MarketStatus { get; set; }

        [JsonProperty("Selections")]
        public List<Selection> Selections { get; set; }

        [JsonProperty("Resourceid")]
        public object Resourceid { get; set; }

        [JsonProperty("Category")]
        public int Category { get; set; }

        [JsonProperty("ShowCategory")]
        public string ShowCategory { get; set; }

        [JsonProperty("Sort")]
        public int Sort { get; set; }

        [JsonProperty("Combo")]
        public int Combo { get; set; }
    }


    public class Selection
    {
        [JsonProperty("Price")]
        public string Price { get; set; }

        [JsonProperty("AllPrice")]
        public AllPrice AllPrice { get; set; }

        [JsonProperty("Key")]
        public string Key { get; set; }

        [JsonProperty("Point")]
        public string Point { get; set; }

        [JsonProperty("Point2")]
        public object Point2 { get; set; }
    }
}
