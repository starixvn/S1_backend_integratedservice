using ThirdPartyBetHistory;
using ThirdPartyBetHistory.Extensions;
using ThirdPartyBetHistory.Service.Worker;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseSentry();

builder.Environment.EnvironmentName = builder.Configuration.GetSection("env").Get<string>();

var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
builder.Configuration.AddRemoteJsonConfig(remoteUrls);

var excludesMessage = builder.Configuration.GetSection("ExcludesMessage").Get<string[]>();
var excludesApiEndpoints = builder.Configuration.GetSection("ExcludesApiEndpoint").Get<string[]>();

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

SentrySdk.CaptureMessage($"Started result common service at {DateTime.Now.ToString()}");

app.Run();