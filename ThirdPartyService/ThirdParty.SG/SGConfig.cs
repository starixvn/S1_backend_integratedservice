using Newtonsoft.Json;
using ThirdPartyCore.Model;

namespace ThirdParty.SG
{
    internal class SGConfig : TpSiteConfig
    {
        public string FunGameUrl { get; set; }

        public string merchantCode { get; set; }

    }

    public class BetCostResponse
    {
        [JsonProperty("serialNo")]
        public string SerialNo { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }

        [JsonProperty("merchantCode")]
        public string MerchantCode { get; set; }

        [JsonProperty("betCosts")]
        public BetCosts BetCosts { get; set; }
    }
    public class BetCosts
    {
        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("betCosts")]
        public IEnumerable<BetCost> BetCostList { get; set; }
    }

    public class BetCost
    {
        [JsonProperty("gameCode")]
        public string GameCode { get; set; }
        [JsonProperty("credit")]
        public int Credit { get; set; }
        [JsonProperty("betCost")]
        public IEnumerable<double> BetCostList { get; set; }
    }

    public class LoyalFreeSpinResponse
    {
        [JsonProperty("serialNo")]
        public string SerialNo { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }

        [JsonProperty("merchantCode")]
        public string MerchantCode { get; set; }

        [JsonProperty("promotionId")]
        public string PromotionId { get; set; }
    }
}
