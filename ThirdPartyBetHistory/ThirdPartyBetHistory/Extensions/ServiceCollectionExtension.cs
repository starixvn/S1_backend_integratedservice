using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPartyBetHistory.Service;
using ThirdPartyBetHistory.Service.BLL;
using ThirdPartyBetHistory.Service.DAL;
using ThirdPartyBetHistory.Service.Library;
using ThirdPartyBetHistory.Service.Model.enums;
using ThirdPartyBetHistory.Service.Worker;

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
            var tp = Tp.Saba;
            var provider = services.BuildServiceProvider();
            var sabaSiteList = provider.GetRequiredService<ThirdPartyBLL>().GetTpList().Where(item => item.Id == (int)tp);
            var type = Type.GetType($"ThirdPartyBetHistory.Service.TpBetHistory.SabaBetHelper");

            foreach (var site in sabaSiteList)
            {        
                services.AddSingleton(provider =>
                {
                    var TpBetHelper = (IThirdPartyBetHelper)Activator.CreateInstance(type, new object[] { provider });
                    TpBetHelper.Tp = tp;
                    TpBetHelper.thirdPartyModel = site;
                    TpBetHelper.syncParams = TpBetHelper.InitialSyncParams();
                    TpBetHelper.SiteId = site.SiteId == 0 ? SiteId.Nova88 : (SiteId)site.SiteId;
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
