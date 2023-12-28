namespace ThirdPartyCore.Model
{
    public class SiteLanguage
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; }
        public Dictionary<string, Dictionary<string, string>> SiteLanguageKeys;

        public SiteLanguage(ServerInfoObject serverinfo, IEnumerable<SiteLanguageObject> siteLanguageObjects)
        {
            SiteId = serverinfo.SiteId;
            SiteName = serverinfo.SiteName;

            SiteLanguageKeys = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var siteLanguage in siteLanguageObjects.GroupBy(i => i.LanguageKey))
            {
                var dictionary = siteLanguage.ToDictionary(i => i.LangId, i => i.LanguageValue);
                SiteLanguageKeys.Add(siteLanguage.Key,
                    new Dictionary<string, string>(dictionary, StringComparer.OrdinalIgnoreCase));
            }
        }
    }
}