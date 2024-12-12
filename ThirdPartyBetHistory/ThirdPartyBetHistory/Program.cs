using ThirdPartyBetHistory;
using ThirdPartyBetHistory.Extensions;
using ThirdPartyBetHistory.Service.Worker;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseSentry();
var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
builder.Configuration.AddRemoteJsonConfig(remoteUrls);

var excludesMessage = builder.Configuration.GetSection("ExcludesMessage").Get<string[]>();
var excludesResponseEmptyMessage = builder.Configuration.GetValue<string>("ExcludesResponseEmptyMessage");
var excludesApiEndpoints = builder.Configuration.GetSection("ExcludesApiEndpoint").Get<string[]>();

//builder.Host.UseSerilog((context, Configuration) =>
//{
//    Configuration
//        .ReadFrom.Configuration(builder.Configuration)
//        .Enrich.FromLogContext()
//        .Filter.ByExcluding(c =>
//            c.Properties.Any(p =>
//                excludesApiEndpoints.Any(item => p.Value.ToString().Contains(item))
//                || excludesMessage.Any(item => c.MessageTemplate.ToString().Contains(item)
//                                               || c.MessageTemplate.ToString().Equals(excludesResponseEmptyMessage))))
//        .Enrich.WithExceptionDetails()
//        .Enrich.WithProperty("ApplicationName", nameof(ThirdPartyBetHistory))
//        .Enrich.WithProperty("Env", builder.Configuration.GetValue<string>("OpenSearch:Env"))
//        .WriteTo.Console()
//        .WriteTo.UDPSink(
//            builder.Configuration.GetValue<string>("OpenSearch:Uri"),
//            builder.Configuration.GetValue<int>("OpenSearch:Port"),
//            new Serilog.Formatting.Json.JsonFormatter())
//        .WriteTo.Sentry(o =>
//        {
//            // Debug and higher are stored as breadcrumbs (default is Information)
//            o.MinimumBreadcrumbLevel = LogEventLevel.Debug;
//            // Warning and higher is sent as event (default is Error)
//            o.MinimumEventLevel = LogEventLevel.Error;
//            o.Dsn = builder.Configuration.GetValue<string>("Sentry:Dsn");
//            o.Environment = builder.Configuration.GetValue<string>("Sentry:Environment");
//            o.SetBeforeSend(@event =>
//            {
//                @event.SetFingerprint(Guid.NewGuid().ToString());
//                return @event;
//            });
//        });
//});
var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

builder.Services
    .AddHostedService<WorkerService>()
    .AddHostedService<SabaByTransIdUnSettleWorkerService>();
    //.AddHostedService<SabaVgamingUnSettleWorkerService>();

var app = builder.Build();
startup.Configure(app, builder.Environment, builder.Configuration);

var minWorkers = Environment.ProcessorCount * 4;
var minIOCPs = Environment.ProcessorCount * 4;
ThreadPool.SetMinThreads(minWorkers, minIOCPs);

SentrySdk.CaptureMessage($"Started {nameof(ThirdPartyBetHistory)} at {DateTime.Now}");

app.Run();