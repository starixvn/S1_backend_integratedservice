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
        public static IServiceCollection AddDatabaseConnection(this IServiceCollection services, IConfiguration configuration)
        {
            var provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<MySqlClient>>();
            var serviceType = Environment.GetEnvironmentVariable("ServiceType") ?? configuration.GetValue<string>("ServiceType");
            return services
            .AddSingleton(param => new MySqlClient(configuration.GetConnectionString($"{serviceType}_MainDB"), logger, DataBase.Main))
            .AddSingleton(param => new MySqlClient(configuration.GetConnectionString($"{serviceType}_ReportDB"), logger, DataBase.Report));
        }

        public static IServiceCollection RegisterSabaSiteHelper(this IServiceCollection services)
        {
            var tp = Tp.SBSW;
            var provider = services.BuildServiceProvider();
            var sabaSiteConfigList = provider.GetRequiredService<ThirdPartyBLL>().GetTpConfigList(tp.ToString());
            var type = Type.GetType($"ThirdPartyBetHistory.Service.TpBetHistory.SabaBetHelper");

            foreach (var siteConfig in sabaSiteConfigList)
            {
                services.AddSingleton(provider =>
                {
                    var TpBetHelper = (IThirdPartyBetHelper)Activator.CreateInstance(type, new object[] { provider });
                    TpBetHelper.Tp = tp;                 
                    TpBetHelper.SiteId = siteConfig.SiteId;
                    TpBetHelper.SBSWConfig = JsonConvert.DeserializeObject<SBSWConfig>(siteConfig.ConfigValue);
                    return TpBetHelper;
                });
            }

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
    }
}
