using ThirdPartyCore.Model;

namespace ThirdParty.Joker
{
    internal class JokerConfig : TpSiteConfig
    {
        public Dictionary<string, CurrencyDataJoker> CurrencyData { get; set; }
    }

    public class CurrencyDataJoker
    {
        public string AppID { get; set; }
        public string Secret { get; set; }
        public string AppID_Test { get; set; }
        public string Secret_Test { get; set; }
    }
}
