using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Manual
{
    internal class Program
    {
        private static readonly string RootPath = Directory.GetCurrentDirectory();
        private static string _env;
        internal static async Task Main(string[] args)
        {
            _env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "PROD";
            Console.WriteLine($"Environment:{_env}");
            var host = CreateHostBuilder(args)
                .Build();

            Console.Write("Please input TpId:");
            var input = Console.ReadLine();
            var tpId = int.Parse(input);

            using var scope = host.Services.CreateScope();
            switch ((Tp)tpId)
            {
                case Tp.Iconic:
                    var iconic = scope.ServiceProvider.GetService<WagerIconic>();
                    await iconic.GetWagerAsync();
                    break;
                default:
                    Console.WriteLine($"No this TpId:{tpId}");
                    break;
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(BuildConfiguration)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDatabaseConnection(hostContext.Configuration);
                    services.AddHttpClientProxy(hostContext.Configuration);
                    services.RegisterComponent();
                });

        private static readonly Action<IConfigurationBuilder> BuildConfiguration =
            builder => builder
                .SetBasePath(RootPath)
                .AddYamlFile("appsettings.json", false, true)
                .AddYamlFile($"appsettings.{_env}.json", false, true)
                .AddEnvironmentVariables();
    };

}