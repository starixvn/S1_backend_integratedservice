using ThirdPartyCore.Model;

namespace ThirdParty.SBPLottery
{
    internal class SBPLotteryConfig : TpSiteConfig
    {
        public string OperatorPwd { get; set; }
        public string OperatorKey { get; set; }
        public string GameType { get; set; }
    }
}