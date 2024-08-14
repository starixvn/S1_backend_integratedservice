using ThirdPartyCore.Model;

namespace ThirdParty.SabaPlay
{
    internal class SabaPlayConfig : TpSiteConfig
    {
        public string OperatorPwd { get; set; }
        public string OperatorKey { get; set; }
        public string GameType { get; set; }
    }
}