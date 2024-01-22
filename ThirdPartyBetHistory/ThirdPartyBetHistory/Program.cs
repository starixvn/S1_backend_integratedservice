using Serilog;
using Serilog.Exceptions;

using ThirdPartyBetHistory;
using ThirdPartyBetHistory.Extensions;
using ThirdPartyBetHistory.Service.Worker;

var builder = WebApplication.CreateBuilder(args);

var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, args, remoteUrls);
builder.Configuration.AddRemoteJsonConfig(remoteUrls);

var excludesMessage = builder.Configuration.GetSection("ExcludesMessage").Get<string[]>();
var excludesApiEndpoints = builder.Configuration.GetSection("ExcludesApiEndpoint").Get<string[]>();
builder.Host.UseSerilog((context, Configuration) =>
{
    Configuration
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Filter.ByExcluding(c =>
            c.Properties.Any(p =>
                excludesApiEndpoints.Any(item => p.Value.ToString().Contains(item))
                || excludesMessage.Any(item => c.MessageTemplate.ToString().Contains(item))))
        .Enrich.WithExceptionDetails();
});

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

builder.Services
    .AddHostedService<WorkerService>()
    .AddHostedService<SabaByTransIdUnSettleWorkerService>()
    .AddHostedService<RngWorkerService>()
    .AddHostedService<SabaVgamingUnSettleWorkerService>();

var app = builder.Build();
startup.Configure(app, builder.Environment);

var minWorkers = Environment.ProcessorCount * 4;
var minIOCPs = Environment.ProcessorCount * 4;
ThreadPool.SetMinThreads(minWorkers, minIOCPs);

app.Run();

