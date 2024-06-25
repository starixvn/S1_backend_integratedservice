using ThirdPartyCore.Model;

namespace ThirdParty.Skywind
{
    internal class SkywindConfig : TpSiteConfig
    {
        public string MerchId { get; set; }
        public string MerchPwd { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }
        public string SecretKey { get; set; }
        public string HistoryUrl { get; set; }
        public string StagingLobbies { get; set; }
    }
}
