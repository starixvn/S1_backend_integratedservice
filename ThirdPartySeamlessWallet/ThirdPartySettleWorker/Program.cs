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

builder.Services.AddHostedService<BonusWorker>();
builder.Services.AddHostedService<SettleWorker>();
builder.Services.AddHostedService<TpReRequestWorker>();
builder.Services.AddHostedService<JokerWorker>();
builder.Services.AddHostedService<MGWorker>();
builder.Services.AddHostedService<APWorker>();
builder.Services.AddHostedService<PPWorker>();
builder.Services.AddHostedService<PGWorker>();
builder.Services.AddHostedService<YGGWorker>();
builder.Services.AddHostedService<TCGWorker>();
builder.Services.AddHostedService<HBWorker>();
builder.Services.AddHostedService<MegaWorker>();
builder.Services.AddHostedService<AMBWorker>();
builder.Services.AddHostedService<SAWorker>();
builder.Services.AddHostedService<SGWorker>();
builder.Services.AddHostedService<AllbetWorker>();
builder.Services.AddHostedService<PTWorker>();
builder.Services.AddHostedService<SBOWorker>();
builder.Services.AddHostedService<JILIWorker>();
builder.Services.AddHostedService<ATWorker>();
builder.Services.AddHostedService<KMWoker>();

var app = builder.Build();
startup.Configure(app, builder.Environment);

app.Run();
