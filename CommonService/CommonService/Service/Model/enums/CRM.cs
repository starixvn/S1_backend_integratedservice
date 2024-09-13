using Newtonsoft.Json;
using System.Globalization;

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
        registration,
        deposit_successful,
        withdrawal_sucessful,
        deposit_initiated,
        withdrawal_initiated
    }
    public class CRMRequest
    {
        public CRMEvent Event { get; set; }
        public int CustId { get; set; }
        public int SiteId { get; set; }
        public decimal OrderAmount { get; set; }
        public int CurrencyId { get; set; }
        public string Environment { get; set; }
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
        [JsonConverter(typeof(DecimalJsonConverter))]
        public decimal order_amount { get; set; }
        public string event_device_type { get; set; } = string.Empty;
        public bool event_native_mobile { get; set; }
        public string event_platform { get; set; } = string.Empty;
        public string event_os { get; set; } = string.Empty;
        public int currencyid { get; set; }
        public int siteid { get; set; }

        public bool ShouldSerializeorder_amount()
        {
            // Return false if order_amount is 0, true otherwise
            return order_amount != 0;
        }
    }

    public class DecimalJsonConverter : JsonConverter<decimal>
    {
        public override void WriteJson(JsonWriter writer, decimal value, JsonSerializer serializer)
        {
            // Convert decimal to string and format it to remove trailing zeros after the decimal point
            string decimalString = value % 1 == 0
                ? value.ToString("0", CultureInfo.InvariantCulture)  // No decimal part if it's an integer
                : value.ToString(CultureInfo.InvariantCulture);       // Keep decimal part if it's not an integer

            writer.WriteValue(decimalString);
        }

        public override decimal ReadJson(JsonReader reader, Type objectType, decimal existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return Convert.ToDecimal(reader.Value, CultureInfo.InvariantCulture);
        }
    }

}
