using ThirdPartyCore.Model;

namespace ThirdParty.Live22
{
    internal class Live22Config : TpSiteConfig
    {
        public string OperatorPwd { get; set; }
        public string OperatorKey { get; set; }
        public string GameType { get; set; }
    }
}