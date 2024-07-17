using ThirdPartyCore.Model;

namespace ThirdParty.SimplePlay
{
    internal class SimplePlayConfig : TpSiteConfig
    {
        public string Lobby { get; set; }
        public string MD5Key { get; set; }
        public string EncryptKey { get; set; }
        public string SecretKey { get; set; }
    }
}
