using System.Collections.Generic;
using System.Linq.Expressions;

using Microsoft.Extensions.DependencyInjection.Extensions;

using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddDatabaseConnection(this IServiceCollection services, IConfiguration config)
        {
            var provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<MySqlClient>>();
            var serviceType = Environment.GetEnvironmentVariable("ServiceType") ?? config.GetValue<string>("ServiceType");

            return services
               .AddSingleton(param => new MySqlClient(config.GetConnectionString($"{serviceType}_MainDB"), logger)
               {
                   DataBase = DataBase.Main
               })
               .AddSingleton(param => new MySqlClient(config.GetConnectionString($"{serviceType}_ReportDB"), logger)
               {
                   DataBase = DataBase.Report
               })
               .AddSingleton(param => new MySqlClient(config.GetConnectionString($"{serviceType}_WagerDB"), logger)
               {
                   DataBase = DataBase.Wager
               });
        }

        public static IServiceCollection AddOptionListByDB<T>(this IServiceCollection services, string sql, object parameters = null, DataBase dataBase = DataBase.Report)
        {
            var provider = services.BuildServiceProvider();
            var configService = provider.GetRequiredService<ConfigService>();
            var option = configService.Query<T>(sql, parameters, dataBase);
            services.AddSingleton(typeof(IEnumerable<T>), p => option);
            return services;
        }

        public static IServiceCollection AddOptionByDB<T>(this IServiceCollection services, string sql, object parameters = null, DataBase dataBase = DataBase.Report)
        {
            var provider = services.BuildServiceProvider();
            var configService = provider.GetRequiredService<ConfigService>();
            var option = configService.Query<T>(sql, parameters, dataBase).FirstOrDefault();
            services.AddSingleton(typeof(T), p => option);
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
                if (thirdPartyModel.Status == 1)
                {
                    tpFunc.Value.Invoke(services);
                }            
            }

            return services;
        }
    }
}
