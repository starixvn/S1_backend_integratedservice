namespace ThirdPartyWager.Service.Model.TpSiteConfig;

internal class PG2Config : TpSiteConfig
{
    public Dictionary<string, CurrencyDataPG2> CurrencyData { get; set; }
}

public class CurrencyDataPG2
{
    public string ApiKey { get; set; }
    public string Agent { get; set; }
}