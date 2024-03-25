namespace ThirdPartyWager.Service.Model.TpSiteConfig
{
    public class NTConnectConfig : TpSiteConfig
    {
        public string SiteName { get; set; }
        public int SiteId { get; set; }
        public List<CurrencyList> CurrencyList { get; set; }
    }

    public class CurrencyList
    {
        public string CurrencyCode { get; set; }
        public string ClientKey { get; set; }
        public string SecretKey { get; set; }
    }
}
        