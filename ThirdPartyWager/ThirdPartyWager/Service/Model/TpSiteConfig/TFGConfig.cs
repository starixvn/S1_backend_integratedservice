using static ThirdPartyWager.Service.BLL.ApiMGBLL;

namespace ThirdPartyWager.Service.Model.TpSiteConfig
{
    public class TFGConfig : TpSiteConfig
    {
        public string RetrieveUrl { get; set; }
        public Dictionary<string, CurrencyDataTFG> CurrencyData { get; set; }
    }
    public class CurrencyDataTFG
    {
        public string PartnerId { get; set; }
        public string PublicToken { get; set; }
        public string PrivateToken { get; set; }
    }
}
