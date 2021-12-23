using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Exceptions;
using System;
using System.Linq;
using ThirdPartySeamlessWallet;
using ThirdPartySeamlessWallet.Service.Workers;
using ThirdPartySettleWorker.Model.Interfaces;
using ThirdPartySettleWorker.Workers;

namespace ThirdPartySettleWorker
{
    public class Program
    {
        private static IConfigurationRoot Configuration
            => new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddEnvironmentVariables()
            .AddJsonFile($"appsettings.json", false, true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", false, true)
            .Build();

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
               .ConfigureWebHostDefaults(webBuilder =>
               {
                   var excludesMessage = Configuration.GetSection("ExcludesMessage").Get<string[]>();
                   var excludesApiEndpoints = Configuration.GetSection("ExcludesApiEndpoint").Get<string[]>();

                   webBuilder.UseSerilog((hostContext, loggerConfiguration) =>
                   {
                       loggerConfiguration
                           .ReadFrom.Configuration(Configuration)
                           .Enrich.FromLogContext()
                           .Filter.ByExcluding(c =>
                               c.Properties.Any(p =>
                                   excludesApiEndpoints.Any(item => p.Value.ToString().Contains(item))
                                   || excludesMessage.Any(item => c.MessageTemplate.ToString().Contains(item))))
                           .Enrich.WithExceptionDetails();
                   })
                   .UseStartup<Startup>();
               })
               .ConfigureServices((hostContext, services) =>
               {
                   // ThirdPartySeamlessWallet.SettleWorker
                   //services.AddHostedService<BonusWorker>();
                   //services.AddHostedService<SettleWorker>();

                   //ISettleWorker
                   //var settleWorkers = typeof(ISettleWorker).Assembly.GetTypes().Where(item => item.GetInterfaces().Contains(typeof(ISettleWorker)));
                   //foreach (var worker in settleWorkers)
                   //{
                   //    services.AddHostedService(provider => (ISettleWorker)Activator.CreateInstance(worker, new object[] { provider }));
                   //}
                   services.AddHostedService<JokerWorker>();
                   services.AddHostedService<MGWorker>();
                   services.AddHostedService<APWorker>();
               });              
    }
}
