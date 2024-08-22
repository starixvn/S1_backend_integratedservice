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
    public class CRMRequest
    {
        public string Event { get; set; }
        public int CustId { get; set; }
        public bool EventNativeMobile { get; set; }
        public int SiteId { get; set; }
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
        [JsonProperty("event_device_type")]
        public string EventDeviceType { get; set; }

        [JsonProperty("event_native_mobile")]
        public bool EventNativeMobile { get; set; }

        [JsonProperty("event_platform")]
        public string EventPlatform { get; set; }

        [JsonProperty("event_os")]
        public string EventOs { get; set; }

        [JsonProperty("order_amount")]
        public int OrderAmount { get; set; }
    }
}
