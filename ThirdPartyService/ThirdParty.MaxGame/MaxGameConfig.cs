using ThirdPartyCore.Model;

namespace ThirdParty.MaxGame
{
    internal class MaxGameConfig : TpSiteConfig
    {
        public string OperatorPwd { get; set; }
        public string OperatorKey { get; set; }
        public string GameType { get; set; }
    }
}