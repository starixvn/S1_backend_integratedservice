namespace ThirdPartyWager.Service.Model.TpSiteConfig
{
    public class AllBetConfig
    {
        public string ApiUrl { get; set; }
        public string Suffix { get; set; }
        public string AllbetKey { get; set; }
        public string OperatorId { get; set; }
        public string PartnerKey { get; set; }
        public List<CurrencyAgent> CurrencyAgents { get; set; }
    }

    public class CurrencyAgent
    {
        public string CurrencyId { get; set; }
        public string Agent { get; set; }
    }
}