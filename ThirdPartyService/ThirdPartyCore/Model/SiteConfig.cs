namespace ThirdPartyCore.Model
{
    public class SiteConfig
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; }
        public Dictionary<string, string> Config { get; set; }

        public SiteConfig(ServerInfoObject serverinfo, Dictionary<string, string> config)
        {
            SiteId = serverinfo.SiteId;
            SiteName = serverinfo.SiteName;
            Config = config;
        }
    }
}
