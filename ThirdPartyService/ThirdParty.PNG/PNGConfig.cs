namespace ThirdParty.PNG
{
    internal class PNGConfig
    {
        public string ApiUrl { get; set; }

        public string Password { get; set; }

        public string UserName { get; set; }

        public string AccessToken { get; set; }

        public string GameUrl { get; set; }

        public string GameHistoryMobile { get; set; }

        public string GameHistoryDesktop { get; set; }

        public int Pid { get; set; }

        public PNGConfig(Dictionary<string, string> siteConfig)
        {
            ApiUrl = siteConfig.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(siteConfig["ApiUrl"])
                ? siteConfig["ApiUrl"]
                : throw new Exception("PlayNGo TpList not found ApiUrl Setting");

            Password = siteConfig.ContainsKey("Password") && !string.IsNullOrEmpty(siteConfig["Password"])
                ? siteConfig["Password"]
                : throw new Exception("PlayNGo TpList not found Password Setting");

            UserName = siteConfig.ContainsKey("UserName") && !string.IsNullOrEmpty(siteConfig["UserName"])
                ? siteConfig["UserName"]
                : throw new Exception("PlayNGo TpList not found UserName Setting");

            AccessToken = siteConfig.ContainsKey("AccessToken") && !string.IsNullOrEmpty(siteConfig["AccessToken"])
                ? siteConfig["AccessToken"]
                : throw new Exception("PlayNGo TpList not found AccessToken Setting");

            GameUrl = siteConfig.ContainsKey("GameUrl") && !string.IsNullOrEmpty(siteConfig["GameUrl"])
                ? siteConfig["GameUrl"]
                : throw new Exception("PlayNGo TpList not found GameLaunchUrl Setting");

            GameHistoryMobile = siteConfig.ContainsKey("GameHistoryMobile") && !string.IsNullOrEmpty(siteConfig["GameHistoryMobile"])
                ? siteConfig["GameHistoryMobile"]
                : throw new Exception("PlayNGo TpList not found GameHistoryMobile Setting");

            GameHistoryDesktop = siteConfig.ContainsKey("GameHistoryDesktop") && !string.IsNullOrEmpty(siteConfig["GameHistoryDesktop"])
                ? siteConfig["GameHistoryDesktop"]
                : throw new Exception("PlayNGo TpList not found GameHistoryDesktop Setting");

            Pid = siteConfig.ContainsKey("Pid") && !string.IsNullOrEmpty(siteConfig["Pid"])
                ? int.Parse(siteConfig["Pid"])
                : throw new Exception("PlayNGo TpList not found Pid Setting");
        }
    }
}
