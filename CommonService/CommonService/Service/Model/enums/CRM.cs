using System;
using Newtonsoft.Json;

namespace CommonService.Service.Model.enums
{
    public class CRM
    {
    }
    public class CRMConfig
    {
        public string ApiUrl { get; set; }

        public int TenantID { get; set; }

        public string Token { get; set; }
    }
    public enum CRMEvent
    {
        Register_successful,
        Deposit_successful
    }
    public class CRMRequest
    {
        public CRMEvent Event { get; set; }
        public int CustId { get; set; }
        public int SiteId { get; set; }
        public decimal OrderAmount { get; set; }
    }

    public class CRMEventData
    {
        [JsonProperty("tenant")]
        public int Tenant { get; set; }

        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("context")]
        public Context Context { get; set; }

        [JsonProperty("visitor")]
        public string Visitor { get; set; }

        [JsonProperty("customer")]
        public string Customer { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
    }

    public class Context
    {
        [JsonProperty("order_amount")]
        public decimal OrderAmount { get; set; }
    }
}
