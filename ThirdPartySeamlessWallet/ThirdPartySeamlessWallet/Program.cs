using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Json;
using ThirdPartySeamlessWallet;
using ThirdPartySeamlessWallet.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

builder.Environment.EnvironmentName = builder.Configuration.GetSection("env").Get<string>();

var remoteUrls = GetRemoteUrls();
builder.Configuration.AddRemoteJsonConfig(remoteUrls);

var excludesMessage = builder.Configuration.GetSection("ExcludesMessage").Get<string[]>();
var excludesResponseEmptyMessage = builder.Configuration.GetValue<string>("ExcludesResponseEmptyMessage");
var excludesApiEndpoints = builder.Configuration.GetSection("ExcludesApiEndpoint").Get<string[]>();

builder.Host.UseSerilog((context, Configuration) =>
{
    Configuration
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Filter.ByExcluding(c =>
            c.Properties.Any(p =>
                excludesApiEndpoints.Any(item => p.Value.ToString().Contains(item))
                || excludesMessage.Any(item => c.MessageTemplate.ToString().Contains(item)
                || c.MessageTemplate.ToString().Equals(excludesResponseEmptyMessage))))
        .Enrich.WithExceptionDetails()
        .Enrich.WithProperty("ApplicationName", "ThirdPartySeamlessWallet")
        .WriteTo.Sentry(o =>
        {
            // Debug and higher are stored as breadcrumbs (default is Information)
            o.MinimumBreadcrumbLevel = LogEventLevel.Debug;
            // Warning and higher is sent as event (default is Error)
            o.MinimumEventLevel = LogEventLevel.Error;
            o.Dsn = builder.Configuration.GetValue<string>("Sentry:Dsn");
            o.Environment = builder.Configuration.GetValue<string>("Sentry:Environment");
            o.SetBeforeSend(@event =>
            {
                @event.SetFingerprint(Guid.NewGuid().ToString());
                return @event;
            });
        });
});

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();
startup.Configure(app, builder.Environment);

const int minWorkers = 96;
const int minIOCPs = 4;
ThreadPool.SetMinThreads(minWorkers, minIOCPs);

SentrySdk.CaptureMessage($"Started {nameof(ThirdPartySeamlessWallet)} {builder.Environment.EnvironmentName} api at {DateTime.Now}");

app.Run();
return;

void OverrideLoggerWriteToLFile(LoggerConfiguration configuration)
{
    const string cashSiteBrand = "Nova";

    var brand = Environment.GetEnvironmentVariable("Brand") ?? string.Empty;
    if (brand != cashSiteBrand) return;

    var logFilePath = $"/seamlesswallet/seamless-{DateTime.Now:yyyyMMddHHmmssfff}.log";
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