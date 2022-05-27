using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Json;
using ThirdPartyBetHistory.Service.Worker;

namespace ThirdPartyBetHistory
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
                                .SetBasePath(Directory.GetCurrentDirectory())
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
                }).ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<WorkerService>();
                    services.AddHostedService<BonusWorker>();
                    services.AddHostedService<SabaLGWorkerService>();
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
