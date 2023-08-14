using CommonService.Service.DAL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

using Newtonsoft.Json;

namespace CommonService.Extensions;

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
           })
           .AddSingleton(param => new MySqlClient(config.GetConnectionString($"{serviceType}_LogDB"), logger)
           {
               DataBase = DataBase.Log
           });
    }

    public static IServiceCollection AddOptionListByDB<T>(this IServiceCollection services, string sql, object parameters = null)
    {
        var provider = services.BuildServiceProvider();
        var configService = provider.GetRequiredService<ConfigService>();
        var option = configService.Query<T>(sql, parameters);
        services.AddSingleton(typeof(IEnumerable<T>), p => option);
        return services;
    }

    public static IServiceCollection AddSiteConfig(this IServiceCollection services, IEnumerable<string> remoteUrls)
    {
        var provider = services.BuildServiceProvider();
        var serverInfoList = new List<ServerInfo>() { new ServerInfo() { SiteId = 0, SiteName = "default" } };
        serverInfoList.AddRange(provider.GetServices<ServerInfo>());

        foreach (var serverInfo in serverInfoList)
        {
            using var client = new HttpClient();
            Stream result = null;
            foreach (var url in remoteUrls)
            {
                if (string.IsNullOrEmpty(url))
                {
                    continue;
                }

                try
                {
                    var configUrl = $"{url}config/{serverInfo.SiteId}";
                    result = client.GetAsync(configUrl).Result.Content.ReadAsStreamAsync().Result;
                    using StreamReader reader = new(result);
                    var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());
                    services.AddSingleton(new SiteConfig(serverInfo, dic));
                    break;
                }
                catch
                {
                    continue;
                }
            }
        }
        return services;
    }
}