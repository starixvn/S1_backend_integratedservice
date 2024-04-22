using Newtonsoft.Json;

namespace CommonService.Service.Model
{
    #region GAConfig

    public class GAConfig
    {
        public string ApiUrl { get; set; }

        public string MeasurementId { get; set; }

        public string SecretKey { get; set; }
    }

    #endregion

    #region PurchaseEventDto

    public class PurchaseEventDto
    {
        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("events")]
        public List<Event> Events { get; set; }
    }

    public class Event
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("params")]
        public Params Params { get; set; }
    }

    public class Params
    {
        /// <summary>
        /// 只支援 ISO 格式 ex：USD、MYR (必填)
        /// </summary>
        [JsonProperty("currency")]
        public string Currency { get; set; }

        /// <summary>
        /// 事件金額 => 對應存款金額 (必填)
        /// </summary>
        [JsonProperty("value")]
        public decimal Value { get; set; }

        /// <summary>
        /// 交易的專屬 ID => 對應存款單號 (必填)
        /// </summary>
        [JsonProperty("transaction_id")]
        public string TransactionId { get; set; }

        /// <summary>
        /// 事件項目 (必填)
        /// </summary>
        [JsonProperty("items")]
        public List<Item> Items { get; set; }
    }

    public class Item
    {
        /// <summary>
        /// 商品的ID  (必填)
        /// </summary>
        [JsonProperty("item_id")]
        public string ItemId { get; set; }

        /// <summary>
        /// 商品名稱 (必填)
        /// </summary>
        [JsonProperty("item_name")]
        public string ItemName { get; set; }
    }

    #endregion

    #region CustomerEventDto

    public class CustomizedEventDto
    {
        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("events")]
        public List<CustomizedEvent> Events { get; set; }
    }

    public class CustomizedEvent
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("params")]
        public Dictionary<string, string> EventParams { get; set; }
    }

    #endregion

    public class PurchaseRequest
    {
        /// <summary>
        /// 站台Id
        /// </summary>
        public int SiteId { get; set; }

        /// <summary>
        /// 用戶iD
        /// </summary>
        public int CustId { get; set; }

        /// <summary>
        /// Deposit TransId
        /// </summary>
        public string TransId { get; set; }

        /// <summary>
        /// 實際存款金額
        /// </summary>
        public decimal DepositAmount { get; set; }

        /// <summary>
        /// 幣別 ex： MYR、USD
        /// </summary>
        public string CurrencyName { get; set; }

        /// <summary>
        /// 商品Id
        /// </summary>
        public string ItemId { get; set; }

        /// <summary>
        /// 商品名稱
        /// </summary>
        public string ItemName { get; set; }

        /// <summary>
        /// 事件名稱
        /// </summary>
        public string EventName { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            return GetType().GetProperties()
                .Where(p => p.CanRead)
                .ToDictionary(p => p.Name, p => p.GetValue(this).ToString());
        }
    }
}
