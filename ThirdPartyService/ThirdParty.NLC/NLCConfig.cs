namespace ThirdParty.NLC
{
    internal class NLCConfig
    {
        public string EcId { get; set; }
        public string ApiUrl { get; set; }
        public string Ua2Token { get; set; }
        public string AuthToken { get; set; }
        public string CasinoKey { get; set; }
        public string GameProvider { get; set; }
        public string GameHistoryApiToken { get; set; }

        public NLCConfig(Dictionary<string, string> siteConfig)
        {
            ApiUrl = siteConfig.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(siteConfig["ApiUrl"]) ? siteConfig["ApiUrl"]: throw new Exception("ApiUrl");
            Ua2Token = siteConfig.ContainsKey("Ua2Token") && !string.IsNullOrEmpty(siteConfig["Ua2Token"]) ? siteConfig["Ua2Token"] : throw new Exception("Ua2Token");
            CasinoKey = siteConfig.ContainsKey("CasinoKey") && !string.IsNullOrEmpty(siteConfig["CasinoKey"]) ? siteConfig["CasinoKey"] : throw new Exception("CasinoKey");
            GameHistoryApiToken = siteConfig.ContainsKey("gameHistoryApiToken") && !string.IsNullOrEmpty(siteConfig["gameHistoryApiToken"]) ? siteConfig["gameHistoryApiToken"] : throw new Exception("gameHistoryApiToken");
        }
    }
}
