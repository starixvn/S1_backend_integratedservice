using Microsoft.AspNetCore.Builder;

using Serilog;
using Serilog.Exceptions;

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
});

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

builder.Services.AddHostedService<TpReRequestWorker>();
builder.Services.AddHostedService<TpRewardWorker>();
builder.Services.AddHostedService<JokerWorker>();
builder.Services.AddHostedService<MGWorker>();
builder.Services.AddHostedService<APWorker>();
builder.Services.AddHostedService<PPWorker>();
builder.Services.AddHostedService<PGWorker>();
builder.Services.AddHostedService<YGGWorker>();
builder.Services.AddHostedService<TCGWorker>();
builder.Services.AddHostedService<HBWorker>();
//builder.Services.AddHostedService<MegaWorker>();
builder.Services.AddHostedService<AMBWorker>();
builder.Services.AddHostedService<SAWorker>();
builder.Services.AddHostedService<SimplePlayWorker>();
builder.Services.AddHostedService<SGWorker>();
builder.Services.AddHostedService<AllbetWorker>();
builder.Services.AddHostedService<PlayTechWorker>();
builder.Services.AddHostedService<SBOWorker>();
builder.Services.AddHostedService<JILIWorker>();
//builder.Services.AddHostedService<ATWorker>();
builder.Services.AddHostedService<KMWoker>();
//builder.Services.AddHostedService<OneGamingWorker>();
builder.Services.AddHostedService<V8Worker>();
builder.Services.AddHostedService<MPWorker>();
builder.Services.AddHostedService<CQ9Worker>();
builder.Services.AddHostedService<GPIWorker>();
builder.Services.AddHostedService<NetentWorker>();
builder.Services.AddHostedService<RedTigerWorker>();
builder.Services.AddHostedService<YLWorker>();
builder.Services.AddHostedService<Kiss918Worker>();
builder.Services.AddHostedService<SkywindWorker>();
builder.Services.AddHostedService<BTIWorker>();
builder.Services.AddHostedService<NineWWorker>();
builder.Services.AddHostedService<One789Worker>();
builder.Services.AddHostedService<GPIWorker>();
builder.Services.AddHostedService<YeebetWorker>();
builder.Services.AddHostedService<BGWorker>();
builder.Services.AddHostedService<TFGWorker>();
builder.Services.AddHostedService<NextspinWorker>();
builder.Services.AddHostedService<ENDWorker>();
builder.Services.AddHostedService<IMWorker>();
builder.Services.AddHostedService<SPRIBEWorker>();
builder.Services.AddHostedService<SBSWWorker>();
builder.Services.AddHostedService<DGWorker>();
builder.Services.AddHostedService<AGWorker>();
builder.Services.AddHostedService<EBetWorker>();
builder.Services.AddHostedService<PNGWorker>();
builder.Services.AddHostedService<SexyWorker>();
builder.Services.AddHostedService<WMWorker>();
builder.Services.AddHostedService<FaChaiWorker>();
builder.Services.AddHostedService<EVOWorker>();
builder.Services.AddHostedService<N2LiveWorker>();
builder.Services.AddHostedService<N2LiveSmartWorker>();
builder.Services.AddHostedService<PegasusWorker>();

var app = builder.Build();
startup.Configure(app, builder.Environment);

const int minWorkers = 32;
const int minIOCPs = 4;
ThreadPool.SetMinThreads(minWorkers, minIOCPs);

app.Run();
