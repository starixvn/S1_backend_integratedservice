using Newtonsoft.Json;

using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class ConfigBLL
    {
        private readonly Dictionary<string, string> _defaultSiteConfig;
        private readonly Dictionary<int, SiteConfig> _siteIdConfigs;

        public ConfigBLL(IServiceProvider provider)
        {
            var siteConfigList = provider.GetServices<SiteConfig>();
            _defaultSiteConfig = siteConfigList.First(i => i.SiteId == 0).Config;

            //TpWager不排除0，需要從site=0拿config            
            _siteIdConfigs = siteConfigList.ToDictionary(i => i.SiteId, i => i);
        }

        public TItem GetConfigValue<TItem>(string configKey, int siteId = 0)
        {
            var defaultItem = _defaultSiteConfig.TryGetValue(configKey, out var json)
                ? JsonConvert.DeserializeObject<TItem>(json) : default;

            if (siteId == 0)
            {
                return defaultItem;
            }

            if (_siteIdConfigs.TryGetValue(siteId, out var siteConfig))
            {
                return siteConfig.Config.TryGetValue(configKey, out var configValue)
                   ? JsonConvert.DeserializeObject<TItem>(configValue) : defaultItem;
            }

            return defaultItem;
        }

        public Dictionary<int, TItem> GetConfigValuesBySiteId<TItem>(string configKey)
        {
            var defaultItem = _defaultSiteConfig.TryGetValue(configKey, out var json)
                ? JsonConvert.DeserializeObject<TItem>(json) : default;

            var result = new Dictionary<int, TItem>();
            foreach (var siteConfig in _siteIdConfigs)
            {
                var data = siteConfig.Value.Config.TryGetValue(configKey, out var configValue)
                    ? JsonConvert.DeserializeObject<TItem>(configValue) : defaultItem;
                result.Add(siteConfig.Key, data);
            }

            return result;
        }
    }
}

