using ThirdPartyCore.Model;

namespace ThirdParty.WM
{
    internal class WMConfig : TpSiteConfig
    {
        public Dictionary<string, CurrencyDataWM> CurrencyData { get; set; }
    }

    public class CurrencyDataWM
    {
        public string vendorId { get; set; }
        public string signature { get; set; }
    }
}
