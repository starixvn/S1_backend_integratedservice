using CommonService.Service.Model;

using Newtonsoft.Json;

namespace CommonService.Service.BLL
{
    public class ConfigBLL
    {
        private readonly SiteConfig _defaultSiteConfig;
        private readonly Dictionary<string, SiteConfig> _siteNameConfigs;
        private readonly Dictionary<int, SiteConfig> _siteIdConfigs;

        public ConfigBLL(IServiceProvider provider)
        {
            var siteConfigList = provider.GetServices<SiteConfig>();
            _defaultSiteConfig = siteConfigList.First(i => i.SiteId == 0);
            _siteNameConfigs = siteConfigList.ToDictionary(i => i.SiteName, i => i);
            _siteIdConfigs = siteConfigList.ToDictionary(i => i.SiteId, i => i);
        }

        public string GetConfigValue(string siteName, string configKey)
        {
            if (_siteNameConfigs.TryGetValue(siteName, out var siteConfig))
            {
                return siteConfig.Config.TryGetValue(configKey, out var configValue) ? configValue : string.Empty;
            }
            else
            {
                return _defaultSiteConfig.Config.TryGetValue(configKey, out var configValue) ? configValue : string.Empty;
            }
        }

        public string GetConfigValue(int siteId, string configKey)
        {
            if (_siteIdConfigs.TryGetValue(siteId, out var siteConfig))
            {
                return siteConfig.Config.TryGetValue(configKey, out var configValue) ? configValue : string.Empty;
            }
            else
            {
                return _defaultSiteConfig.Config.TryGetValue(configKey, out var configValue) ? configValue : string.Empty;
            }
        }

        public Dictionary<int, TItem> GetConfigValuesBySiteId<TItem>(string configKey)
        {
            var result = new Dictionary<int, TItem>();
            foreach (var siteConfig in _siteIdConfigs)
            {
                if (siteConfig.Value.Config.TryGetValue(configKey,out var configValue))
                {
                    var data = JsonConvert.DeserializeObject<TItem>(configValue);
                    result.Add(siteConfig.Key, data);
                }
            }
            return result;
        }

        public Dictionary<string, TItem> GetConfigValuesBySiteName<TItem>(string configKey)
        {
            var result = new Dictionary<string, TItem>();
            foreach (var siteConfig in _siteNameConfigs)
            {
                if (siteConfig.Value.Config.TryGetValue(configKey, out var configValue))
                {
                    var data = JsonConvert.DeserializeObject<TItem>(configValue);
                    result.Add(siteConfig.Key, data);
                }
            }
            return result;
        }
    }
}
