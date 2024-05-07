using ThirdPartyCore.Model;

namespace ThirdParty.MG
{
    internal class MGConfig : TpSiteConfig
    {
        public string TokenUrl { get; set; }
        public Dictionary<string, CurrencyDataMG> CurrencyData { get; set; }
    }

    public class CurrencyDataMG
    {
        public string MasterAgent { get; set; }
        public string AgentCode { get; set; }
        public string SecretKey { get; set; }
    }
}
