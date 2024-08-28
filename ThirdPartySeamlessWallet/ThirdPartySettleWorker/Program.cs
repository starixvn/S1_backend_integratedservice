using Microsoft.AspNetCore.Builder;

using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Json;
using ThirdPartySeamlessWallet;
using ThirdPartySeamlessWallet.Core.Extensions;

using ThirdPartySettleWorker.Workers;

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

    OverrideLoggerWriteToLFile(Configuration);
});

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

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

builder.Services
    .AddHostedService<TpReRequestWorker>()
    .AddHostedService<TpRewardWorker>()
    .AddHostedService<JokerWorker>()
    .AddHostedService<MGWorker>()
    .AddHostedService<APWorker>()
    .AddHostedService<PPWorker>()
    .AddHostedService<PGWorker>()
    .AddHostedService<YGGWorker>()
    .AddHostedService<TCGWorker>()
    .AddHostedService<HBWorker>()
    .AddHostedService<AMBWorker>()
    .AddHostedService<SAWorker>()
    .AddHostedService<SimplePlayWorker>()
    .AddHostedService<SGWorker>()
    .AddHostedService<AllbetWorker>()
    .AddHostedService<PlayTechWorker>()
    .AddHostedService<SBOWorker>()
    .AddHostedService<JILIWorker>()
    //.AddHostedService<ATWorker>()
    .AddHostedService<KMWoker>()
    //.AddHostedService<OneGamingWorker>()
    .AddHostedService<V8Worker>()
    .AddHostedService<MPWorker>()
    .AddHostedService<CQ9Worker>()
    .AddHostedService<GPIWorker>()
    .AddHostedService<NetentWorker>()
    .AddHostedService<RedTigerWorker>()
    .AddHostedService<NLCWorker>()
    .AddHostedService<AmigoWorker>()
    //.AddHostedService<YLWorker>()
    .AddHostedService<Kiss918Worker>()
    .AddHostedService<SkywindWorker>()
    .AddHostedService<BTIWorker>()
    .AddHostedService<NineWWorker>()
    .AddHostedService<One789Worker>()
    .AddHostedService<YeebetWorker>()
    .AddHostedService<BGWorker>()
    .AddHostedService<TFGWorker>()
    .AddHostedService<NextspinWorker>()
    .AddHostedService<ENDWorker>()
    .AddHostedService<IMWorker>()
    .AddHostedService<SPRIBEWorker>()
    .AddHostedService<SBSWWorker>()
    .AddHostedService<PNGWorker>()
    .AddHostedService<DGWorker>()
    .AddHostedService<PegasusWorker>()
    .AddHostedService<SexyWorker>()
    .AddHostedService<AGWorker>()
    .AddHostedService<EBetWorker>()
    .AddHostedService<WMWorker>()
    .AddHostedService<FaChaiWorker>()
    .AddHostedService<EVOWorker>()
    //.AddHostedService<N2LiveWorker>()
    //.AddHostedService<N2LiveSmartWorker>()
    .AddHostedService<BNGWorker>()
    .AddHostedService<HLWorker>()
    .AddHostedService<MaxGameWorker>()
    .AddHostedService<SabaPlayWorker>()
    .AddHostedService<UuSlotsWorker>()
    .AddHostedService<Live22Worker>()
    .AddHostedService<FastSpinWorker>()
    .AddHostedService<NTConnectWorker>()
    .AddHostedService<GAWorker>()
    .AddHostedService<WonCasinoWorker>()
    .AddHostedService<MarblesWorker>()
    .AddHostedService<DragonGamingWorker>();

var app = builder.Build();
startup.Configure(app, builder.Environment);

const int minWorkers = 32;
const int minIOCPs = 4;
ThreadPool.SetMinThreads(minWorkers, minIOCPs);

app.Run();
