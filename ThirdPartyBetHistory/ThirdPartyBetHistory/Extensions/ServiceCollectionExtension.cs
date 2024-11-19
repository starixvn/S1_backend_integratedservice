using Newtonsoft.Json;

using ThirdPartyBetHistory.Service;
using ThirdPartyBetHistory.Service.BLL;
using ThirdPartyBetHistory.Service.DAL;
using ThirdPartyBetHistory.Service.Library;
using ThirdPartyBetHistory.Service.Model;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Extensions
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
            return services;
        }

        public static IServiceCollection RegisterSabaSiteHelper(this IServiceCollection services)
        {
            var tp = Tp.SBSW;
            var provider = services.BuildServiceProvider();
            var sabaSiteConfigList = provider.GetRequiredService<ThirdPartyBLL>().GetTpConfigList(tp.ToString());
            var type = Type.GetType($"ThirdPartyBetHistory.Service.TpBetHistory.SabaBetHelper");

            foreach (var siteConfig in sabaSiteConfigList)
            {
                var sabaConfig = JsonConvert.DeserializeObject<SabaConfig>(siteConfig.ConfigValue);
                if (IsSabaConfigValid(sabaConfig))
                {
                    services.AddSingleton(provider =>
                    {
                        var TpBetHelper = (IThirdPartyBetHelper)Activator.CreateInstance(type, new object[] { provider });
                        TpBetHelper.Tp = tp;
                        TpBetHelper.SiteId = siteConfig.SiteId;
                        TpBetHelper.SabaConfig = sabaConfig;
                        return TpBetHelper;
                    });
                }
            }

            return services;
        }

        private static bool IsSabaConfigValid(SabaConfig sabaConfig)
        {
            return sabaConfig != null && !string.IsNullOrWhiteSpace(sabaConfig.ApiUrl);
        }

        public static IServiceCollection AddOptionListByDB<T>(this IServiceCollection services, string sql, object parameters = null)
        {
            var provider = services.BuildServiceProvider();
            var configService = provider.GetRequiredService<ConfigService>();
            var option = configService.Query<T>(sql, parameters);
            services.AddSingleton(typeof(IEnumerable<T>), p => option);
            return services;
        }
    }
}