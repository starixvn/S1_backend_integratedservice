using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.Network;
using ThirdPartyWager;
using ThirdPartyWager.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseSentry();
builder.Environment.EnvironmentName = builder.Configuration.GetSection("env").Get<string>();

var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
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
        .Enrich.WithProperty("ApplicationName", "ThirdPartyWager")
        .WriteTo.TCPSink("tcp://10.18.200.193", 31853, new Serilog.Formatting.Json.JsonFormatter())
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

var app = builder.Build();
startup.Configure(app, builder.Environment);
SentrySdk.CaptureMessage($"Started {nameof(ThirdPartyWager)} {builder.Environment.EnvironmentName} at {DateTime.Now}");

app.Run();