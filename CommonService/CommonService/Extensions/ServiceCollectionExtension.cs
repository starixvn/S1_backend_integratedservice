using CommonService.Service.DAL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using System.Net.Mail;
using System.Net;

namespace CommonService.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddDatabaseConnection(this IServiceCollection services, IConfiguration config)
    {
        var serviceType = Environment.GetEnvironmentVariable("ServiceType") ?? config.GetValue<string>("ServiceType");

        services.AddSingleton(provider => new MySqlClient(provider, DataBase.Main, config.GetConnectionString($"{serviceType}_MainDB")));
        services.AddSingleton(provider => new MySqlClient(provider, DataBase.Log, config.GetConnectionString($"{serviceType}_LogDB")));
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
        var serverInfoList = new List<ServerInfo>() { new ServerInfo() { SiteId = 0, SiteName = "default" } };
        serverInfoList.AddRange(provider.GetRequiredService<IEnumerable<ServerInfo>>());

        // 準備Config
        var siteConfigGroup = provider.GetRequiredService<IEnumerable<Config>>().GroupBy(i => i.SiteId);

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
        var serverInfoList = new List<ServerInfo>() { new ServerInfo() { SiteId = 0, SiteName = "default" } };
        serverInfoList.AddRange(provider.GetRequiredService<IEnumerable<ServerInfo>>());

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

    public static IServiceCollection AddSmtpClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton((sp) =>
        {
            var emailSettings = new EmailSettingsOptions();
            configuration.GetSection("EmailSetting").Bind(emailSettings);

            return new SmtpClient
            {
                Host = emailSettings.SmtpServer,
                Port = emailSettings.Port,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    emailSettings.Username,
                    emailSettings.Password),
                EnableSsl = !string.IsNullOrEmpty(emailSettings.Password)
            };
        });

        return services;
    }
}