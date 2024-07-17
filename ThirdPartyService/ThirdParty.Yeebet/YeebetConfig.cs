using ThirdPartyCore.Model;

namespace ThirdParty.Yeebet
{
    internal class YeebetConfig : TpSiteConfig
    {
        public Dictionary<string, CurrencyDataYeebet> CurrencyData { get; set; }
    }

    public class CurrencyDataYeebet
    {
        public string ApiAppId { get; set; }
        public string ApiSecretKey { get; set; }
        public string SeamlessAppId { get; set; }
        public string SeamlessSecretKey { get; set; }
    }
}
