using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using ThirdPartyService;
using ThirdPartyService.Extensions;
using ThirdPartyService.Service.Worker;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseSentry();
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

builder.Environment.EnvironmentName = builder.Configuration.GetSection("Env").Get<string>();
var remoteUrls = GetRemoteUrls();

builder.Configuration.AddRemoteJsonConfig(remoteUrls);

var excludesMessage = builder.Configuration.GetSection("ExcludesMessage").Get<string[]>();
var excludesApiEndpoints = builder.Configuration.GetSection("ExcludesApiEndpoint").Get<string[]>();

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
startup.Configure(app, builder.Environment);
SentrySdk.CaptureMessage($"Started {nameof(ThirdPartyService)} api at {DateTime.Now}");
app.Run();
return;

void OverrideLoggerWriteToLFile(LoggerConfiguration configuration)
{
    const string cashSiteBrand = "Nova";

    var brand = Environment.GetEnvironmentVariable("Brand") ?? string.Empty;
    if (brand != cashSiteBrand) return;

    var logFilePath = $"/tpservice/tpservice-{DateTime.Now:yyyyMMddHHmmssfff}.log";
    configuration
        .WriteTo.File(new JsonFormatter(),
            logFilePath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 60,
            fileSizeLimitBytes: 10485760,
            rollOnFileSizeLimit: true,
            restrictedToMinimumLevel: LogEventLevel.Information);
}

string[] GetRemoteUrls()
{
    return builder.Configuration.GetSection("ConfigService").Get<string[]>();
}