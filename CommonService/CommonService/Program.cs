using CommonService;
using CommonService.Extensions;
using Serilog;
using Serilog.Exceptions;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

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
        .Enrich.WithExceptionDetails();

    OverrideLoggerWriteToLFile(Configuration);
});

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();
startup.Configure(app, builder.Environment);

app.Run();
return;

void OverrideLoggerWriteToLFile(LoggerConfiguration configuration)
{
    const string cashSiteBrand = "Nova";

    var brand = Environment.GetEnvironmentVariable("Brand") ?? string.Empty;
    if (brand != cashSiteBrand) return;

    var logFilePath = $"/common/common-{DateTime.Now:yyyyMMddHHmmssfff}.log";
    configuration
        .WriteTo.File(new JsonFormatter(),
            logFilePath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 60,
            fileSizeLimitBytes: 10485760,
            rollOnFileSizeLimit: true);
}

string[] GetRemoteUrls()
{
    // �p�G�� Runtime Environment Variable �u���ϥ�
    // �S���h�ϥιw�] AppSetting
    return builder.Configuration.GetSection("ConfigService").Get<string[]>();
}