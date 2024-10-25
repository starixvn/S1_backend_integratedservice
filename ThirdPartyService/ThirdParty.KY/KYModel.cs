using Newtonsoft.Json;
using ThirdPartyCore.Model;

namespace ThirdParty.KY
{
    public class KYConfig : TpSiteConfig
    {
        public string SecureKey { get; set; }
        public string SecureLogin { get; set; }
        public string ApiUrl_Stream { get; set; }
        public string SecureKey_Stream { get; set; }
        public string SecureLogin_Stream { get; set; }
    }
}
