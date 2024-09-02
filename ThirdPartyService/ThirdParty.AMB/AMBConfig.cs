using ThirdPartyCore.Model;

namespace ThirdParty.AMB
{
    public class AMBConfig : TpSiteConfig
    {
        public Dictionary<string, CurrencyDataAMB> PokerCurrencyData { get; set; }
        public Dictionary<string, CurrencyDataAMB> SlotCurrencyData { get; set; }
    }

    public class CurrencyDataAMB
    {
        public string CurrencyId { get; set; }
        public string Agent { get; set; }
        public string ApiUrl { get; set; }
        public string SecretKey { get; set; }
    }
}
