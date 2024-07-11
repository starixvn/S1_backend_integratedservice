using Newtonsoft.Json;
using ThirdPartyCore.Model;

namespace ThirdParty.NineW
{
    internal class NineWConfig : TpSiteConfig
    {
        public string DomainUrl { get; set; }
        public string ExchangeUrl { get; set; }

        [JsonProperty("website")]
        public string WebSite { get; set; }

        [JsonProperty("cert")]
        public string Cert { get; set; }

        [JsonProperty("agent")]
        public string Agent { get; set; }

        public Dictionary<string, CurrencyDataNineW> CurrencyData { get; set; }
    }

    public class CurrencyDataNineW
    {
        [JsonProperty("agent")]
        public string Agent { get; set; }
    }
    
    public class Domain
    {
        public string ApiUrl { get; set; }
        public string ExchangeUrl { get; set; }
    }
}
