using Newtonsoft.Json;

namespace ThirdPartyCore.Model.TpMerchantBalance.Response
{
    public class TpMerchantBalanceResponse
    {
        [JsonProperty("result")]
        public TpMerchantBalanceResult Result { get; set; }
    }

    public class TpMerchantBalanceResult
    {
        /// <summary>
        /// TP商戶餘額，餘額不足會影響玩家轉帳
        /// </summary>
        [JsonProperty("depositbalance")]
        public decimal MerchantBalalce { get; set; }

        /// <summary>
        /// 商戶名稱
        /// </summary>
        [JsonProperty("adminname")]
        public string MerchantName { get; set; }      
    }
}
