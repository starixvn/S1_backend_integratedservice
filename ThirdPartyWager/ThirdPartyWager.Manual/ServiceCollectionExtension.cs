using System;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Manual
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddDatabaseConnection(this IServiceCollection services, IConfiguration config)
        {
            var provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<MySqlClient>>();
            
            return services
                .AddSingleton(param => new MySqlClient(config.GetConnectionString($"ConnectionString"), logger)
                {
                    DataBase = DataBase.Main
                })
                .AddSingleton(param => new MySqlClient(config.GetConnectionString($"ConnectionStringReport"), logger)
                {
                    DataBase = DataBase.Report
                })
                .AddSingleton(param => new MySqlClient(config.GetConnectionString($"ConnectionStringWager"), logger)
                {
                    DataBase = DataBase.Wager
                });
        }

        public static IServiceCollection AddHttpClientProxy(this IServiceCollection services, IConfiguration config)
        {
            if (!string.IsNullOrEmpty(config.GetValue<string>("ProxyHost")))
            {
                var proxy = new HttpClientHandler
                {
                    Proxy = new WebProxy(config.GetValue<string>("ProxyHost"), config.GetValue<int>("ProxyPort")),
                    UseProxy = true
                };

                services.AddHttpClient("with_proxy")
                    .ConfigurePrimaryHttpMessageHandler(() => proxy);
            }
            else
            {
                services.AddHttpClient();
            }

            return services;
        }

        public static IServiceCollection RegisterComponent(this IServiceCollection services)
        {
            services.AddMemoryCache();

            services.AddSingleton<HttpHelper>();
            services.AddSingleton<Common>();
            services.AddSingleton<DateTimeHelper>();
            services.AddSingleton<Log2File>();

            /*DAL*/
            services.AddSingleton<ThirdPartyHelper>();
            services.AddSingleton<CommonService>();
            services.AddSingleton<WagerDGService>();
            services.AddSingleton<WagerAllBetService>();
            services.AddSingleton<WagerSAService>();
            services.AddSingleton<WagerAGService>();
            services.AddSingleton<WagerSGService>();
            services.AddSingleton<WagerWMService>();
            services.AddSingleton<WagerKiss918Service>();
            services.AddSingleton<WagerOne789Service>();
            services.AddSingleton<WagerMegaService>();
            services.AddSingleton<WagerIconicService>();
            services.AddSingleton<WagerOneGamingService>();
            services.AddSingleton<WagerV8Service>();
            services.AddSingleton<WagerN2LiveService>();
            services.AddSingleton<WagerPTService>();
            services.AddSingleton<LiveStreamingService>();
            services.AddSingleton<LatestHighlightVideoService>();
            services.AddSingleton<SabaOddsService>();
            services.AddSingleton<WagerKMService>();
            services.AddSingleton<WagerMPService>();
            services.AddSingleton<WagerEVOService>();
            services.AddSingleton<TpUnsettleService>();
            services.AddSingleton<ConfigService>();
            services.AddSingleton<FincanceService>();

            /*BLL*/
            services.AddSingleton<ThirdPartyBLL>();
            // Iconic
            services.AddSingleton<WagerIconicBLL>();
            services.AddSingleton<ApiIconicBLL>();
            
            services.AddSingleton<WagerIconic>();
            return services;
        }
    }
}