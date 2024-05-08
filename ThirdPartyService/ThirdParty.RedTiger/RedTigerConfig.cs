using ThirdPartyCore.Model;

namespace ThirdParty.RedTiger
{
    internal class RedTigerConfig : TpSiteConfig
    {
        public string ApiUrl_Backup { get; set; }
        public string EcId { get; set; }
        public string Ua2Token { get; set; }
        public string AuthToken { get; set; }
        public string CasinoKey { get; set; }
        public string GameProvider { get; set; }
        public string GameHistoryApiToken { get; set; }
    }
}
