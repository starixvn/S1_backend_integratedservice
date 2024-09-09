namespace ThirdPartyWager.Service.Model.TpSiteConfig
{
    public class BGConfig : TpSiteConfig
    {
        public Dictionary<string, CurrencyDataBG> CurrencyData { get; set; }
    }
    public class CurrencyDataBG
    {
        public string Sn { get; set; }
        public string AgentId { get; set; }
        public string AgentLoginId { get; set; }
        public string ApiSecretKey { get; set; }
        public string AgentPassword { get; set; }
    }
}
