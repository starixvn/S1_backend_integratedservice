namespace ThirdPartyWager.Service.Model.TpSiteConfig
{
    public class AMBConfig
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