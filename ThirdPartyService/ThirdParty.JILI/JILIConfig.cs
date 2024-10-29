using ThirdPartyCore.Model;

namespace ThirdParty.JILI
{
    internal class JILIConfig : TpSiteConfig
    {
        public string OperatorPwd { get; set; }
        public string OperatorKey { get; set; }
        public string GameType { get; set; }
    }
}
