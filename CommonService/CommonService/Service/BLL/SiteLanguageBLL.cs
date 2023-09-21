using System.Security.Policy;

using CommonService.Service.Model;

using static Elastic.Apm.Config.ConfigConsts;

namespace CommonService.Service.BLL
{
    public class SiteLanguageBLL
    {
        private readonly SiteLanguage _defaultSiteLanguage;
        private readonly Dictionary<string, SiteLanguage> _siteNameLanguages;
        private readonly Dictionary<int, SiteLanguage> _siteIdLanguages;

        public SiteLanguageBLL(IServiceProvider provider)
        {
            var siteLanguageList = provider.GetServices<SiteLanguage>();

            _defaultSiteLanguage = siteLanguageList.First(i => i.SiteId == 0);

            var siteLanguages = siteLanguageList.Where(i => i.SiteId != 0);
            _siteNameLanguages = new Dictionary<string, SiteLanguage>(siteLanguages.ToDictionary(i => i.SiteName, i => i), StringComparer.OrdinalIgnoreCase);
            _siteIdLanguages = siteLanguages.ToDictionary(i => i.SiteId, i => i);
        }

        public string GetLanguageValue(string siteName, string languageKey, string langId)
        {
            try
            {
                var defaultDic = _defaultSiteLanguage.SiteLanguageKeys.GetValueOrDefault(languageKey);
                var defaultValue = defaultDic.TryGetValue(langId, out var langValue) ? langValue : defaultDic["en"];

                if (_siteNameLanguages.TryGetValue(siteName, out var siteLanguage))
                {
                    var siteLanguageDic = siteLanguage.SiteLanguageKeys.GetValueOrDefault(languageKey);
                    return siteLanguageDic.TryGetValue(langId, out var result) ? result : defaultDic["en"];
                }
                return defaultValue;
            }
            catch
            {

                return string.Empty;
            }         
        }

        public string GetLanguageValue(string siteName, int languageCode, string langId)
        {
            return string.Empty;
        }

        public string GetLanguageValue(int siteId, string languageKey, string langId)
        {
            return string.Empty;
        }

        public string GetLanguageValue(int siteId, int languageCode, string langId)
        {
            return string.Empty;
        }


    }
}
