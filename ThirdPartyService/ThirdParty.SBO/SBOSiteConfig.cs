using ThirdPartyCore.Model;

namespace ThirdParty.SBO
{
    internal class SBOConfig : TpSiteConfig
    {
        public string CompanyKey { get; set; }
        public string Agent { get; set; }
        public string ServerId { get; set; }
    }
}
