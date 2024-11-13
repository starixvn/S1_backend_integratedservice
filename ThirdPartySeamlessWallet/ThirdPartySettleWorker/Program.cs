using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Json;
using ThirdPartySeamlessWallet;
using ThirdPartySeamlessWallet.Core.Extensions;
using ThirdPartySettleWorker.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseSentry(option =>
{
    option.SetBeforeSend(@event =>
    {
        @event.SetFingerprint(Guid.NewGuid().ToString());
        return @event;
    });
});

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

builder.Environment.EnvironmentName = builder.Configuration.GetSection("env").Get<string>();

var remoteUrls = GetRemoteUrls();
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
        .Enrich.WithExceptionDetails()
        .WriteTo.MySQL(builder.Configuration.GetValue<string>("connectionStrings:worker_LogDB"), "LogInfo")
        .WriteTo.Sentry(o =>
        {
            // Debug and higher are stored as breadcrumbs (default is Information)
            o.MinimumBreadcrumbLevel = LogEventLevel.Debug;
            // Warning and higher is sent as event (default is Error)
            o.MinimumEventLevel = LogEventLevel.Error;
            o.Dsn = builder.Configuration.GetValue<string>("Sentry:Dsn");
            o.Environment = builder.Configuration.GetValue<string>("Sentry:Environment");
        });
});

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

builder.Services
    .AddHostedService<TpReRequestWorker>()
    .AddHostedService<TpRewardWorker>()
    //.AddHostedService<JokerWorker>()
    //.AddHostedService<MGWorker>()
    //.AddHostedService<APWorker>()
    .AddHostedService<PGWorker>()
    //.AddHostedService<YGGWorker>()
    //.AddHostedService<TCGWorker>()
    //.AddHostedService<HBWorker>()
    //.AddHostedService<AMBWorker>()
    //.AddHostedService<SAWorker>()
    //.AddHostedService<SimplePlayWorker>()
    //.AddHostedService<PlayTechWorker>()
    //.AddHostedService<ATWorker>()
    //.AddHostedService<KMWoker>()
    //.AddHostedService<OneGamingWorker>()
    //.AddHostedService<V8Worker>()
    //.AddHostedService<MPWorker>()
    //.AddHostedService<CQ9Worker>()
    //.AddHostedService<GPIWorker>()
    //.AddHostedService<NetentWorker>()
    //.AddHostedService<RedTigerWorker>()
    //.AddHostedService<NLCWorker>()
    //.AddHostedService<AmigoWorker>()
    //.AddHostedService<YLWorker>()
    //.AddHostedService<Kiss918Worker>()
    //.AddHostedService<SkywindWorker>()
    //.AddHostedService<BTIWorker>()
    //.AddHostedService<NineWWorker>()
    //.AddHostedService<One789Worker>()
    //.AddHostedService<YeebetWorker>()
    //.AddHostedService<BGWorker>()
    //.AddHostedService<TFGWorker>()
    //.AddHostedService<NextspinWorker>()
    //.AddHostedService<ENDWorker>()
    //.AddHostedService<IMWorker>()
    //.AddHostedService<SPRIBEWorker>()
    //.AddHostedService<PNGWorker>()
    //.AddHostedService<DGWorker>()
    //.AddHostedService<PegasusWorker>()
    //.AddHostedService<AGWorker>()
    .AddHostedService<EBetWorker>()
    //.AddHostedService<WMWorker>()
    //.AddHostedService<EVOWorker>()
    //.AddHostedService<N2LiveWorker>()
    //.AddHostedService<N2LiveSmartWorker>()
    //.AddHostedService<BNGWorker>()
    //.AddHostedService<HLWorker>()
    //.AddHostedService<NTConnectWorker>()
    //.AddHostedService<GAWorker>()
    //.AddHostedService<WonCasinoWorker>()
    //.AddHostedService<SGWorker>()
    .AddHostedService<PPWorker>()
    .AddHostedService<AllbetWorker>()
    .AddHostedService<SexyWorker>()
    //.AddHostedService<SBOWorker>()
    .AddHostedService<RNGKenoWorker>()
    .AddHostedService<JILIWorker>()
    .AddHostedService<FaChaiWorker>()
    .AddHostedService<SabaPlayWorker>()
    .AddHostedService<UuSlotsWorker>()
    .AddHostedService<Live22Worker>()
    .AddHostedService<FastSpinWorker>()
    .AddHostedService<SBSWWorker>()
    .AddHostedService<MaxGameWorker>()
    .AddHostedService<LotteryWorker>();
    //.AddHostedService<TableGameWorker>();

var app = builder.Build();
startup.Configure(app, builder.Environment);

const int minWorkers = 32;
const int minIOCPs = 4;
ThreadPool.SetMinThreads(minWorkers, minIOCPs);

SentrySdk.CaptureMessage($"Started {nameof(ThirdPartySettleWorker)} {builder.Environment.EnvironmentName} at {DateTime.Now}");

app.Run();
return;

void OverrideLoggerWriteToLFile(LoggerConfiguration configuration)
{
    const string cashSiteBrand = "Nova";

    var brand = Environment.GetEnvironmentVariable("Brand") ?? string.Empty;
    if (brand != cashSiteBrand) return;

    var logFilePath = $"/settleworker/settleworker-{DateTime.Now:yyyyMMddHHmmssfff}.log";
    configuration
        .WriteTo.File(new JsonFormatter(),
            logFilePath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 60,
            fileSizeLimitBytes: 10485760,
            rollOnFileSizeLimit: true,
            restrictedToMinimumLevel: LogEventLevel.Warning);
}

string[] GetRemoteUrls()
{
    return builder.Configuration.GetSection("ConfigService").Get<string[]>();
}