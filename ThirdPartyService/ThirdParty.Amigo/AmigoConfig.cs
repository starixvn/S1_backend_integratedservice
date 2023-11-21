using ThirdPartyCore.Model;

namespace ThirdParty.Amigo
{
    internal class AmigoConfig : TpSiteConfig
    {
        public string Brand { get; set; }
        public string SecretKey { get; set; }
        public int StyleName { get; set; }
        public string FunGameUrl { get; set; }
    }
}
