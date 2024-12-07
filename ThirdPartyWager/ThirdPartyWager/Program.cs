using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.Network;
using ThirdPartyWager;
using ThirdPartyWager.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseSentry();

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
        .Enrich.WithProperty("ApplicationName", nameof(ThirdPartyWager))
        .Enrich.WithProperty("Env", builder.Configuration.GetValue<string>("OpenSearch:Env"))
        .WriteTo.UDPSink(
            builder.Configuration.GetValue<string>("OpenSearch:Uri"),
            builder.Configuration.GetValue<int>("OpenSearch:Port"),
            new Serilog.Formatting.Json.JsonFormatter())
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
startup.Configure(app, builder.Environment, builder.Configuration);
SentrySdk.CaptureMessage($"Started {nameof(ThirdPartyWager)} at {DateTime.Now}");

app.Run();