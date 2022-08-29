using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Exceptions;
using ThirdPartySeamlessWallet;
using ThirdPartySeamlessWallet.Core.Extensions;
using ThirdPartySeamlessWallet.Service.Workers;
using ThirdPartySettleWorker.Workers;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddRemoteJsonConfig(builder.Configuration.GetSection("ConfigService").Get<string[]>());

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
    .AddHostedService<BonusWorker>()
    .AddHostedService<SettleWorker>()
    .AddHostedService<TpReRequestWorker>()
    .AddHostedService<JokerWorker>()
    .AddHostedService<MGWorker>()
    .AddHostedService<APWorker>()
    .AddHostedService<PPWorker>()
    .AddHostedService<PGWorker>()
    .AddHostedService<YGGWorker>()
    .AddHostedService<TCGWorker>()
    .AddHostedService<HBWorker>()
    .AddHostedService<MegaWorker>()
    .AddHostedService<AMBWorker>()
    .AddHostedService<SAWorker>()
    .AddHostedService<SGWorker>()
    .AddHostedService<AllbetWorker>()
    .AddHostedService<PTWorker>()
    .AddHostedService<SBOWorker>()
    .AddHostedService<JILIWorker>()
    .AddHostedService<ATWorker>()
    .AddHostedService<SkywindWorker>()
    .AddHostedService<KMWoker>()
    .AddHostedService<OneGamingWorker>()
    .AddHostedService<V8Worker>()
    .AddHostedService<MPWorker>()
    .AddHostedService<CQ9Worker>()
    .AddHostedService<GPIWorker>()
    .AddHostedService<NetentWorker>()
    .AddHostedService<RedTigerWorker>()
    .AddHostedService<YLWorker>();

var app = builder.Build();
startup.Configure(app, builder.Environment);

app.Run();
