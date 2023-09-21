namespace CommonService.Service.Model
{
    public class SiteLanguage
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; }
        public Dictionary<string, Dictionary<string, string>> SiteLanguageKeys;
        public Dictionary<int, Dictionary<string, string>> SiteLanguageCodes;

        public SiteLanguage(ServerInfo serverinfo, IEnumerable<SiteLanguageObject> siteLanguageObjects)
        {
            SiteId = serverinfo.SiteId;
            SiteName = serverinfo.SiteName;

            SiteLanguageKeys = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var siteLanguage in siteLanguageObjects.GroupBy(i => i.LanguageKey))
            {
                SiteLanguageKeys.Add(siteLanguage.Key, new Dictionary<string, string>(siteLanguage.ToDictionary(i => i.LangId, i => i.LanguageValue), StringComparer.OrdinalIgnoreCase)
                    );
            }

            SiteLanguageCodes = new Dictionary<int, Dictionary<string, string>>();
            foreach (var siteLanguage in siteLanguageObjects.GroupBy(i => i.LanguageCode))
            {
                SiteLanguageCodes.Add(siteLanguage.Key, new Dictionary<string, string>(siteLanguage.ToDictionary(i => i.LangId, i => i.LanguageValue), StringComparer.OrdinalIgnoreCase));
            }
        }
    }
}
