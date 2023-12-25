using ThirdPartyCore.Model;

namespace ThirdParty.SA
{
    internal class SAConfig : TpSiteConfig
    {
        public string SecretKey { get; set; }

        public string EncrypKey { get; set; }

        public string Md5Key { get; set; }
    }
}
