using Newtonsoft.Json;
using ThirdPartyCore.Model;

namespace ThirdParty.KY
{
    public class KYConfig : TpSiteConfig
    {
        public string Agent {  get; set; }
    }

    public class LoginResult
    {
        public int error { get; set; }
        public string description { get; set; }
        public string gameURL { get; set; }
    }
}
