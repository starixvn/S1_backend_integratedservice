using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Exceptions;
using Serilog.Formatting.Json;
using ThirdPartyService.Service.Worker;

namespace ThirdPartyService
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

        private static readonly List<string> Excludes = new List<string>()
        {
            "/health",
            "{HostingRequestStartingLog:l}",
            "Executing endpoint '{EndpointName}'",
            "Route matched with {RouteData}.",
            "Executing ObjectResult, writing value of type '{Type}'",
            "Executed action {ActionName}",
            "Executed endpoint '{EndpointName}'",
            "{HostingRequestFinishedLog:l}",
            "Executing ContentResult with HTTP Response ContentType of {ContentType}",
            "Worker running at: {time}"
        };

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) 
            => Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                    .UseSerilog((hostContext, loggerConfiguration) =>
                    {
                        loggerConfiguration
                            .ReadFrom.Configuration(Configuration)
                            .Enrich.FromLogContext()
                            .Filter.ByExcluding(c => c.Properties.Any(p => p.Value.ToString().Contains("/health")
                                || Excludes.Any(item => c.MessageTemplate.ToString().Contains(item))))
                            .Enrich.WithExceptionDetails()
                            .MinimumLevel.Information().WriteTo.Console(new JsonFormatter());
                    })
                    .UseStartup<Startup>();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<WorkerService>();
                });
    }
}
