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
        public int s { get; set; }
        public string m { get; set; }
        public LoginData d { get; set; }
    }

    public class LoginData
    {
        public int code { get; set; }
        public string url { get; set; }
    }
}
