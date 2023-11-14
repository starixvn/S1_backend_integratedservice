namespace ThirdPartyWager.Service.Model.TpSiteConfig
{
    public class AMBConfig
    {
        public string PokerAgent { get; set; }
        public string PokerApiUrl { get; set; }
        public string PokerSecretKey { get; set; }
        public List<SlotCurrencyAgent> SlotCurrencyAgent { get; set; }
    }
    public class SlotCurrencyAgent
    {
        public string CurrencyId { get; set; }
        public string Agent { get; set; }
        public string SlotApiUrl { get; set; }
        public string SecretKey { get; set; }
    }
}