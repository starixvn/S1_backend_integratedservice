using ThirdPartyCore.Model;

namespace ThirdParty.AP
{
    internal class APConfig : TpSiteConfig
    {
        public string DetailSiteCode { get; set; }
        public string SiteCode { get; set; }
        public string SecretKey { get; set; }
        public string MiniGame_Crash { get; set; }
        public string APIKey_MiniGame { get; set; }
        public string GameUrl_MiniGame { get; set; }
        public string WagerUrl_MiniGame { get; set; }
        public string SecretKey_MiniGame { get; set; }
        public string MiniGame_CardRacing { get; set; }
        public string Platform_MiniGame { get; set; }
        public string BrandCode { get; set; }
    }
}
