namespace ThirdPartyWager.Service.Model.TpSiteConfig
{
    public class IMSPRIBEConfig : TpSiteConfig
    {
        public string Key { get; set; }
        public string Prefix { get; set; }
        public string MerchantName { get; set; }
    }
    public class IMConfig : IMSPRIBEConfig
    {
    }
    public class SPRIBEConfig : IMSPRIBEConfig
    {
    }
}
