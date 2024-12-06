using System.Net;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            var serviceType = Environment.GetEnvironmentVariable("ServiceType") ?? config.GetValue<string>("ServiceType");

            services.AddSingleton(provider => new MySqlClient(provider, DataBase.Main, config.GetConnectionString($"{serviceType}_MainDB")));
            services.AddSingleton(provider => new MySqlClient(provider, DataBase.Wager, config.GetConnectionString($"{serviceType}_WagerDB")));
            return services;
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

            services.AddHttpClient();

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
            services.AddSingleton<LiveStreamingService>();
            services.AddSingleton<LatestHighlightVideoService>();
            services.AddSingleton<SabaOddsService>();
            services.AddSingleton<WagerKMService>();
            services.AddSingleton<WagerMPService>();
            services.AddSingleton<WagerEVOService>();
            services.AddSingleton<TpUnsettleService>();
            services.AddSingleton<ConfigService>();
            services.AddSingleton<FinanceService>();

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