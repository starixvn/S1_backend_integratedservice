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
        register_successful,
        deposit_successful
    }
    public class CRMRequest
    {
        public CRMEvent Event { get; set; }
        public int CustId { get; set; }
        public int SiteId { get; set; }
        public decimal OrderAmount { get; set; }
        public int CurrencyId { get; set; }
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
        public string Timestamp { get; set; }
    }

    public class Context
    {
        public decimal order_amount { get; set; }
        public string event_device_type { get; set; } = string.Empty;
        public bool event_native_mobile { get; set; }
        public string event_platform { get; set; } = string.Empty;
        public string event_os { get; set; } = string.Empty;
        public int currencyId { get; set; }
        public int siteId { get; set; }
    }
}
