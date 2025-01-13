using BetDetailApiHost.ApiClient;
using BetDetailApiHost.CronJob;
using BetDetailApiHost.Helper;
using BetDetailApiHost.Repository;
using Refit;

namespace BetDetailApiHost.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHostCore(this IServiceCollection services)
        => services.AddSingleton<IMemoryDb, MemoryDb>()
                    //.AddHostedService<SendBetDetailToSimulator>()
                    .AddSingleton<IBetDetailStorage, BetDetailStorage>()
                    .AddHostedService<SendBetDetailToLocalJob>();

        public static IServiceCollection AddApiClients(this IServiceCollection services, ConfigurationManager configuration)
        {
            var simulatorUrl = configuration.GetConnectionString("Simulator");

            var sabaSportClientConfig = (HttpClient client) =>
            {
                client.BaseAddress = new Uri($"{simulatorUrl}/api/SabaSport");
            };

            services.AddRefitClient<ISabaSportApi>().ConfigureHttpClient(sabaSportClientConfig);

            return services;
        }
    }
}