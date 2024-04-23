namespace ThirdPartyWager.Service.Model.TpSiteConfig
{
    public class WonCasinoConfig : TpSiteConfig
    {
        public string SiteName { get; set; }
        public int SiteId { get; set; }
        public IEnumerable<WonCasinoCurrency> CurrencyList { get; set; }
    }
    public class WonCasinoCurrency
    {
        public string CurrencyCode { get; set; }
        public string AppId { get; set; }
        public string SecretKey { get; set; }
    }
}
