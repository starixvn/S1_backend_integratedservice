using Newtonsoft.Json;
using ThirdPartyCore.Model;

namespace ThirdParty.KM
{
    internal class KMConfig : TpSiteConfig
    {
        [JsonProperty("X-QM-ClientId")]
        public string X_QM_ClientId { get; set; }

        [JsonProperty("X-QM-ClientSecret")]
        public string X_QM_ClientSecret { get; set; }

        public string GameUrl_RMB { get; set; }
    }
}
