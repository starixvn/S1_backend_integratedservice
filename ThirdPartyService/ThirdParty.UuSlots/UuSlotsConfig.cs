using ThirdPartyCore.Model;

namespace ThirdParty.UuSlots
{
    internal class UuSlotsConfig : TpSiteConfig
    {
        public string OperatorPwd { get; set; }
        public string OperatorKey { get; set; }
        public string GameType { get; set; }
    }
}