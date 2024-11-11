using ThirdPartyCore.Model;

namespace ThirdParty.Lottery
{
    internal class LotteryConfig : TpSiteConfig
    {
        public string OperatorPwd { get; set; }
        public string OperatorKey { get; set; }
        public string GameType { get; set; }
    }
}