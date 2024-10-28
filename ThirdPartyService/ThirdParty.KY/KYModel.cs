using Newtonsoft.Json;
using ThirdPartyCore.Model;

namespace ThirdParty.KY
{
    public class KYConfig : TpSiteConfig
    {
        public string Agent {  get; set; }
        public string OperateSubtype { get; set; }
        public string Md5Key { get; set; }
        public string DesKey { get; set; }
    }

    public class LoginResult
    {
        [JsonProperty("s")]
        public int SuboperationType { get; set; }

        [JsonProperty("m")]
        public string MainOperationType { get; set; }

        [JsonProperty("d")]
        public LoginData DataResult { get; set; }
    }

    public class LoginData
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
