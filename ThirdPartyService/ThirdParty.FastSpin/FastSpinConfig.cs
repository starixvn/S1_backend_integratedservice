using ThirdPartyCore.Model;

namespace ThirdParty.FastSpin
{
    internal class FastSpinConfig : TpSiteConfig
    {
        public string OperatorPwd { get; set; }
        public string OperatorKey { get; set; }
        public string GameType { get; set; }
    }
}