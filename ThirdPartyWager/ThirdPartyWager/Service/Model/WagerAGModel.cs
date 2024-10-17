using Newtonsoft.Json;

namespace ThirdPartyWager.Service.Model
{
    public class WagerAGModel
    {
        public Result result { get; set; }
    }

    public class Result
    {
        public int info { get; set; }
        public IList<BetAG> row { get; set; }
        public Addition addition { get; set; }
        public string Code { get; set; }
    }

    public class Addition
    {
        public int total { get; set; }
        public int num_per_page { get; set; }
        public int currentpage { get; set; }
        public int totalpage { get; set; }
        public int perpage { get; set; }
    }

    public class BetAG
    {
        [JsonProperty("@billNo")]
        public string billNo { get; set; }

        [JsonProperty("@playName")]
        public string playName { get; set; }

        [JsonProperty("@gameCode")]
        public string gameCode { get; set; }

        [JsonProperty("@netAmount")]
        public decimal netAmount { get; set; }

        [JsonProperty("@betTime")]
        public DateTime betTime { get; set; }

        [JsonProperty("@betAmount")]
        public decimal betAmount { get; set; }

        [JsonProperty("@validBetAmount")]
        public decimal validBetAmount { get; set; }

        [JsonProperty("@flag")]
        public int flag { get; set; }

        [JsonProperty("@playType")]
        public int playType { get; set; }

        [JsonProperty("@currency")]
        public string currency { get; set; }

        [JsonProperty("@tableCode")]
        public string tableCode { get; set; }

        [JsonProperty("@recalcuTime")]
        public DateTime recalcuTime { get; set; }

        [JsonProperty("@beforeCredit")]
        public decimal beforeCredit { get; set; }

        [JsonProperty("@betIP")]
        public string betIP { get; set; }

        [JsonProperty("@platformType")]
        public string platformType { get; set; }

        [JsonProperty("@remark")]
        public string remark { get; set; }

        [JsonProperty("@round")]
        public string round { get; set; }

        [JsonProperty("@result")]
        public string result { get; set; }

        [JsonProperty("@gameType")]
        public string gameType { get; set; }

        [JsonProperty("@deviceType")]
        public int deviceType { get; set; }

        [JsonProperty("@wagersTotal")]
        public int wagersTotal { get; set; }

        public long betUnix
        {
            get
            {
                return new DateTimeOffset(betTime, new TimeSpan(-4, 0, 0)).ToUnixTimeSeconds();
            }
        }
        public long recalcuUnix
        {
            get
            {
                return new DateTimeOffset(recalcuTime, new TimeSpan(-4, 0, 0)).ToUnixTimeSeconds();
            }
        }
    }

    public class WagerAGTotal
    {
        public int WagerCount { get; set; }
        public int SiteId { get; set; }
        public int MainSiteId { get; set; }
        public string TpCurrencyId { get; set; }
    }

    public class CurrencyStatistics_AG
    {
        public int MainSiteId { get; set; }
        public int WagerCount { get; set; }

    }

    public class BettransDataAG
    {
        public string transactionID { get; set; }
        public string currency { get; set; }
        public decimal value { get; set; }
        public string playname { get; set; }
        public DateTime betTime { get; set; }
        public string round { get; set; }
        public string gametype { get; set; }
        public string gameCode { get; set; }
        public string tableCode { get; set; }
        public string transactionCode { get; set; }
        public int playtype { get; set; }
        public int SettleFlag { get; set; }
        public int TicketType { get; set; }
        public decimal Amount => (currency == "IDR" || currency == "VND") ? value / 1000 : value;
    }

    public class TicketDataAG
    {
        public TicketResult result { get; set; }
    }

    public class TicketResult
    {
        public IList<TicketRowAG> row { get; set; }
    }

    public class TicketRowAG
    {
        [JsonProperty("@billNo")]
        public string billNo { get; set; }

        [JsonProperty("@status")]
        public string status { get; set; }
    }

    public class SeamlessAgResponse
    {
        public TransferAgResponse TransferResponse { get; set; }

    }

    public class TransferAgResponse
    {
        public string ResponseCode { get; set; }
        public decimal Balance { get; set; }
    }
}
