namespace ThirdPartyWager.Service.Model.TpSiteConfig;

public class DragonGamingConfig : TpSiteConfig
{
    public string SiteName { get; set; }
    public int SiteId { get; set; }
    public IEnumerable<WonCasinoCurrency> CurrencyList { get; set; }
}
