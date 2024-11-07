using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Workers;

namespace ThirdPartyWager.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddDatabaseConnection(this IServiceCollection services, IConfiguration config)
        {
            var serviceType = Environment.GetEnvironmentVariable("ServiceType") ?? config.GetValue<string>("ServiceType");

            services.AddSingleton(provider => new MySqlClient(provider, DataBase.Main, config.GetConnectionString($"{serviceType}_MainDB")));
            services.AddSingleton(provider => new MySqlClient(provider, DataBase.Wager, config.GetConnectionString($"{serviceType}_WagerDB")));
            return services;
        }

        public static IServiceCollection AddOptionListByDB<T>(this IServiceCollection services, string sql, object parameters = null, DataBase dataBase = DataBase.Main)
        {
            var provider = services.BuildServiceProvider();
            var configService = provider.GetRequiredService<ConfigService>();
            var option = configService.Query<T>(sql, parameters, dataBase);
            services.AddSingleton(typeof(IEnumerable<T>), p => option);
            return services;
        }

        public static IServiceCollection AddSiteConfig(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            // 準備ServerInfo
            var serverInfoList = new List<ServerInfo>() { new ServerInfo() { SiteId = 0, SiteName = "default" } };
            serverInfoList.AddRange(provider.GetRequiredService<IEnumerable<ServerInfo>>());

            // 準備Config
            var siteConfigGroup = provider.GetRequiredService<IEnumerable<ConfigObject>>().GroupBy(i => i.SiteId);

            foreach (var serverInfo in serverInfoList)
            {
                //設定SiteConfig
                var siteConfig = siteConfigGroup.FirstOrDefault(i => i.Key == serverInfo.SiteId) ?? siteConfigGroup.First(i => i.Key == 0);
                var config = siteConfig.ToDictionary(i => i.ConfigKey, i => i.ConfigValue);
                services.AddSingleton(new SiteConfig(serverInfo, config));
            }

            return services;
        }

        public static IServiceCollection AddTpList(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            var tpHelper = provider.GetRequiredService<ThirdPartyHelper>();
            var tpFuncDictionary = new TpExtension().TpFuncDictionary;

            foreach (var tpFunc in tpFuncDictionary)
            {
                var thirdPartyModel = tpHelper.GetThirdPartySetting((int)tpFunc.Key);
                if (thirdPartyModel != null && thirdPartyModel.Status == 1)
                {
                    tpFunc.Value.Invoke(services);
                }
            }

            return services;
        }

        public static IServiceCollection AddSabaOdds(this IServiceCollection services)
        {
            services.AddSingleton<SabaOddsService>();
            services.AddSingleton<SabaOddsBLL>();
            services.AddHostedService<SabaOdds>();
            return services;
        }

        public static IServiceCollection AddSabaGameDetail(this IServiceCollection services)
        {
            services.AddSingleton<SabaGameDetailService>();
            services.AddSingleton<SabaGameDetailBLL>();
            services.AddHostedService<SabaGameDetail>();
            return services;
        }

        public static IServiceCollection AddLiveStreaming(this IServiceCollection services)
        {
            services.AddSingleton<LiveStreamingService>();
            services.AddSingleton<LiveStreamingBLL>();
            services.AddHostedService<LiveStreaming>();
            return services;
        }

        public static IServiceCollection AddLatestHighlightVideo(this IServiceCollection services)
        {
            services.AddSingleton<LatestHighlightVideoService>();
            services.AddSingleton<LatestHighlightVideoBLL>();
            services.AddHostedService<LatestHighlightVideo>();
            return services;
        }
    }
}
