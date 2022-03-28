using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using ThirdPartySeamlessWallet;
using ThirdPartySeamlessWallet.Service.Workers;
using ThirdPartySettleWorker.Workers;

namespace ThirdPartySettleWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
               .ConfigureWebHostDefaults(webBuilder =>
               {
                   webBuilder
                       .ConfigureAppConfiguration((hostContext, builder) =>
                       {
                           builder
                               .SetBasePath(AppContext.BaseDirectory)
                               .AddEnvironmentVariables()
                               .AddJsonFile($"appsettings.json", false, true)
                               .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", false, true);
                           hostContext.Configuration = builder.Build();
                           builder.AddJsonStream(GetRemoteConfig(hostContext.Configuration.GetSection("ConfigService").Get<string[]>()));
                       })
                       .UseSerilog((hostContext, loggerConfiguration) =>
                       {
                           var excludesMessage = hostContext.Configuration.GetSection("ExcludesMessage").Get<string[]>();
                           var excludesApiEndpoints = hostContext.Configuration.GetSection("ExcludesApiEndpoint").Get<string[]>();
                           
                           loggerConfiguration
                               .ReadFrom.Configuration(hostContext.Configuration)
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
                   services.AddHostedService<BonusWorker>();
                   services.AddHostedService<SettleWorker>();
                   
                   services.AddHostedService<JokerWorker>();
                   services.AddHostedService<MGWorker>();
                   services.AddHostedService<APWorker>();
                   services.AddHostedService<PPWorker>();
                   services.AddHostedService<PGWorker>();
                   services.AddHostedService<YGGWorker>();
                   services.AddHostedService<TCGWorker>();
                   services.AddHostedService<HBWorker>();
                   services.AddHostedService<MegaWorker>();
                   services.AddHostedService<SAWorker>();
               });     
        
        private static MemoryStream GetRemoteConfig(IEnumerable<string> remoteUrls)
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
                    result = client.GetAsync(url).Result.Content.ReadAsStreamAsync().Result;
                    
                    break;
                }
                catch { }
            }

            using var streamReader = new StreamReader(result);
            return new MemoryStream(Encoding.UTF8.GetBytes(streamReader.ReadToEnd()));
        }
    }
}
