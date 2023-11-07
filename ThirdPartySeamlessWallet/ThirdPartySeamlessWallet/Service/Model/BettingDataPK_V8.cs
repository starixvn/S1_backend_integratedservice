using Newtonsoft.Json;

using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataPK_V8
    {
        public int s { get; set; }
        public string account { get; set; }
        public string orderId { get; set; }
        public string gameNo { get; set; }
        public string kindId { get; set; }
        public decimal money { get; set; }
        public long timestamp { get; set; }
    }

    public class OrderDataPK_V8
    {
        public int s { get; set; }
        public string orderId { get; set; }
        public long timestamp { get; set; }
    }

    public class BettransDataPK_V8 : BettingDataPK_V8
    {
        public BettransType TicketType { get; set; }
        public TicketStatus TicketStatus { get; set; }
        public SettleFlag SettleFlag { get; set; }
    }

    public class SettleDataPK_V8 : SettleData
    {
        public string GameID { get; set; }
        public int KindID { get; set; }
    }

    public class ApiReq
    {
        public string agent { get; set; }
        public long timestamp { get; set; }
        [JsonProperty("@param")]
        public string param { get; set; }
        public string key { get; set; }
    }

    public class CommonP
    {
        public int s { get; set; }
    }

    public class GetBalanceP : CommonP
    {
        public string account { get; set; }
    }

    public class CurrencyData
    {
        public string Deskey { get; set; }
        public string Agent { get; set; }
        public string Md5key { get; set; }
    }
    public class V8Config
    {
        public string WagerUrl { get; set; }
        public string ApiUrl { get; set; }
        public Dictionary<string, CurrencyData> CurrencyData { get; set; }
    }
}
