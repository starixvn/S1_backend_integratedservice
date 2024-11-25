using ThirdPartyCore.Model;

namespace ThirdParty.WE
{
    internal class WEConfig : TpSiteConfig
    {
        public string OperatorPwd { get; set; }
        public string OperatorKey { get; set; }
        public string GameType { get; set; }
    }
}