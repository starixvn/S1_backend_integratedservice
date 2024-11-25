using ThirdPartyCore.Model;

namespace ThirdParty.AESexy
{
    internal class AESexyConfig : TpSiteConfig
    {
        public string OperatorPwd { get; set; }
        public string OperatorKey { get; set; }
        public string GameType { get; set; }
    }
}