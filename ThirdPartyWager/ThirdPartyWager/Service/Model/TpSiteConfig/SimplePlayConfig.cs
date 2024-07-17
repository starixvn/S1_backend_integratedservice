namespace ThirdPartyWager.Service.Model.TpSiteConfig
{
    public class SimplePlayConfig : TpSiteConfig
    {
        public string Lobby { get; set; }
        public string MD5Key { get; set; }
        public string EncryptKey { get; set; }
        public string SecretKey { get; set; }
    }
}
