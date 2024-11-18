using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

using ThirdPartyService.Service.DAL;
using ThirdPartyService.Service.Library;

namespace ThirdPartyService.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddDatabaseConnection(this IServiceCollection services, IConfiguration config)
        {
            var serviceType = Environment.GetEnvironmentVariable("ServiceType") ?? config.GetValue<string>("ServiceType");
            var reportDBConn = config.GetConnectionString($"{serviceType}_ReportDB");
            var reportBackupDB1 = config.GetConnectionString($"{serviceType}_ReportBackupDB1");
            var reportBackupDB2 = config.GetConnectionString($"{serviceType}_ReportBackupDB2");

            //檢查是否有BackupDB連線字串
            if (!string.IsNullOrEmpty(reportBackupDB1) && !string.IsNullOrEmpty(reportBackupDB2))

            {
                // 設定ReportDB連線順序
                services.AddSingleton(provider => new MySqlClient(provider, DataBase.Report, reportDBConn, new List<string>()
                {
                    reportDBConn,   //1b
                    reportBackupDB1,//1c
                    reportBackupDB2,//1a
                }));

                // 設定ReportBackupDB連線順序
                services.AddSingleton(provider => new MySqlClient(provider, DataBase.ReportBackupDB, reportBackupDB1, new List<string>()
                {
                    reportBackupDB1,//1c
                    reportDBConn,   //1b
                    reportBackupDB2,//1a
                }));
            }
            else
            {
                services.AddSingleton(provider => new MySqlClient(provider, DataBase.Report, reportDBConn));
            }

            services.AddSingleton(provider => new MySqlClient(provider, DataBase.Main, config.GetConnectionString($"{serviceType}_MainDB")));
            services.AddSingleton(provider => new MySqlClient(provider, DataBase.Wager, config.GetConnectionString($"{serviceType}_WagerDB")));
            return services;
        }

        public static IServiceCollection AddOptionListByDB<T>(this IServiceCollection services, string sql, object parameters = null)
        {
            var provider = services.BuildServiceProvider();
            var configService = provider.GetRequiredService<ConfigService>();
            var option = configService.Query<T>(sql, parameters);
            services.AddSingleton(typeof(IEnumerable<T>), p => option);
            return services;
        }

        public static IServiceCollection AddSiteConfig(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            // 準備ServerInfo
            var serverInfoList = new List<ServerInfoObject>() { new ServerInfoObject() { SiteId = 0, SiteName = "default" } };
            serverInfoList.AddRange(provider.GetRequiredService<IEnumerable<ServerInfoObject>>());

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

        public static IServiceCollection AddSiteLanguage(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            // 準備ServerInfo
            var serverInfoList = new List<ServerInfoObject>() { new ServerInfoObject() { SiteId = 0, SiteName = "default" } };
            serverInfoList.AddRange(provider.GetRequiredService<IEnumerable<ServerInfoObject>>());

            // 準備SiteLanguage
            var siteLanguageGroup = provider.GetRequiredService<IEnumerable<SiteLanguageObject>>().GroupBy(i => i.SiteId);

            foreach (var serverInfo in serverInfoList)
            {
                //設定SiteLanguage
                var siteLanguages = siteLanguageGroup.FirstOrDefault(i => i.Key == serverInfo.SiteId) ?? siteLanguageGroup.First(i => i.Key == 0);
                services.AddSingleton(new SiteLanguage(serverInfo, siteLanguages.ToList()));
            }

            return services;
        }

        public static IServiceCollection AddDbApiLog(this IServiceCollection services, IConfiguration configuration)
        {
            const string PayloadLogConfigPath = "PayloadLog";
            return services
                .Configure<PayloadLogConfig>(configuration.GetSection(PayloadLogConfigPath))
                .AddSingleton<IPayloadLogger, PayloadLogService>();
        }

        public static void AddWebProxy(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            var configBll = provider.GetRequiredService<ConfigBLL>();
            var config = configBll.GetConfigValueByEnvironment<ProxyConfig>("TokyoProxy");

            if (!string.IsNullOrWhiteSpace(config.ProxyHost))
            {
                ThirdPartyHelperBase.proxyHost = config.ProxyHost;
                ThirdPartyHelperBase.proxyPort = config.ProxyPort;
            }
        }
    }
}