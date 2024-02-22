using Newtonsoft.Json;
using ThirdPartyCore.Model;

namespace ThirdParty.NineW
{
    internal class NineWConfig : TpSiteConfig
    {
        public string OldApiUrl { get; set; }

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
}
