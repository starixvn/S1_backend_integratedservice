using Microsoft.Extensions.DependencyInjection;
using ThirdPartyCore.Model;

namespace ThirdPartyCore.BLL;

public class SiteLanguageBLL
{
    private readonly Dictionary<string, Dictionary<string, string>> _defaultLanguage;
    private readonly Dictionary<int, SiteLanguage> _siteIdLanguages;

    public SiteLanguageBLL(IServiceProvider provider)
    {
        var siteLanguageList = provider.GetServices<SiteLanguage>();
        _defaultLanguage = siteLanguageList.First(i => i.SiteId == 0).SiteLanguageKeys;

        var siteLanguages = siteLanguageList.Where(i => i.SiteId != 0);
        _siteIdLanguages = siteLanguages.ToDictionary(i => i.SiteId, i => i);
    }

    public string GetLanguageValue(int siteId, string languageKey, string langId)
    {
        try
        {
            var defaultDic = _defaultLanguage.GetValueOrDefault(languageKey);
            var defaultValue = defaultDic.TryGetValue(langId, out var langValue) ? langValue : defaultDic["en"];

            if (_siteIdLanguages.TryGetValue(siteId, out var siteLanguage))
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
}