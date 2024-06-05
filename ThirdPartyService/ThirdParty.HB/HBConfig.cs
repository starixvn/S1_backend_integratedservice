using ThirdPartyCore.Model;

namespace ThirdParty.HB
{
    internal class HBConfig : TpSiteConfig
    {
        public string APIKey { get; set; }
        public string BrandId { get; set; }
        public string LaunchUrl { get; set; }
    }

    public class LaunchUrlResponse
    {
        public bool Success { get; set; }
        public string LaunchURL { get; set; }
        public string Message { get; set; }      
    }
}
