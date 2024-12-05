using Newtonsoft.Json;

using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class ConfigBLL
    {
        private readonly Dictionary<string, string> _defaultSiteConfig;
        private readonly Dictionary<int, SiteConfig> _siteIdConfigs;
        private readonly IConfiguration _configuration;

        public ConfigBLL(IServiceProvider provider, IConfiguration configuration)
        {
            var siteConfigList = provider.GetServices<SiteConfig>();
            _defaultSiteConfig = siteConfigList.First(i => i.SiteId == 0).Config;

            //TpWager不排除0，需要從site=0拿config
            _siteIdConfigs = siteConfigList.ToDictionary(i => i.SiteId, i => i);
            _configuration = configuration;
        }

        public string GetConfigValue(string configKey, int siteId = 0)
        {
            var defaultResult = _defaultSiteConfig.TryGetValue(configKey, out var result) ? result : string.Empty;

            if (siteId == 0)
            {
                return defaultResult;
            }

            if (_siteIdConfigs.TryGetValue(siteId, out var siteConfig))
            {
                return siteConfig.Config.TryGetValue(configKey, out var configValue) ? configValue : defaultResult;
            }

            return defaultResult;
        }

        public TItem GetConfigValue<TItem>(string configKey, int siteId = 0)
        {
            var json = GetConfigValue(configKey, siteId);
            return !string.IsNullOrEmpty(json) ? JsonConvert.DeserializeObject<TItem>(json) : default;
        }

        public string GetEnvironmentName()
        {
            return _configuration.GetValue<string>("EnvironmentName");
        }

        public ServiceHostConfig GetServiceHostConfig()
        {
            var serviceHostConfig = _configuration.GetSection("serviceHosts").Get<ServiceHostConfig>();
            return serviceHostConfig;
        }

        public ProxyConfig GetProxyConfig()
        {
            if (!string.IsNullOrWhiteSpace(_configuration.GetValue<string>("ProxyHost")))
            {
                return new ProxyConfig
                {
                    ProxyHost = _configuration.GetValue<string>("ProxyHost"),
                    ProxyPort = _configuration.GetValue<int>("ProxyPort")
                };
            }

            return null;
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