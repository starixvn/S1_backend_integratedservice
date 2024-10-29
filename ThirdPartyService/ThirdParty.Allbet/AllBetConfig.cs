using ThirdPartyCore.Model;

namespace ThirdParty.Allbet
{
    internal class AllBetConfig : TpSiteConfig
    {
        public string OperatorPwd { get; set; }
        public string OperatorKey { get; set; }
        public string GameType { get; set; }
    }
}