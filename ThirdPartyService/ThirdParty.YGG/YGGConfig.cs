using ThirdPartyCore.Model;

namespace ThirdParty.YGG
{
    internal class YGGConfig : TpSiteConfig
    {
        public string key { get; set; }
        public string org { get; set; }
        public string topOrg { get; set; }
        public string FunGameUrl { get; set; }
    }
}
