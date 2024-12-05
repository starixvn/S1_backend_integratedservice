using ThirdPartyService;
using ThirdPartyService.Extensions;
using ThirdPartyService.Service.Worker;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseSentry();
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

var remoteUrls = GetRemoteUrls();

builder.Configuration.AddRemoteJsonConfig(remoteUrls);

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var serviceType = Environment.GetEnvironmentVariable("ServiceType");
if (!string.IsNullOrEmpty(serviceType) && serviceType.Equals("worker", StringComparison.OrdinalIgnoreCase))
{
    builder.Services
        .AddHostedService<TransferWorkerService>()
        .AddHostedService<SBSWCreateMemberWorker>()
        .AddHostedService<HLCreateMemberWorker>()
        .AddHostedService<CreatePPBonusCodeWorker>();
}

var app = builder.Build();
startup.Configure(app, builder.Environment, builder.Configuration);
SentrySdk.CaptureMessage($"Started {nameof(ThirdPartyService)} api at {DateTime.Now}");
app.Run();
return;

string[] GetRemoteUrls()
{
    return builder.Configuration.GetSection("ConfigService").Get<string[]>();
}