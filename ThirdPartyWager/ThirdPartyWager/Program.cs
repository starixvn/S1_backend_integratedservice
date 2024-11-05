using Serilog;
using Serilog.Exceptions;
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
        .WriteTo.MySQL(builder.Configuration.GetValue<string>("connectionStrings:worker_LogDB"), "LogInfo");
});
var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();
startup.Configure(app, builder.Environment);
SentrySdk.CaptureMessage($"Started {nameof(ThirdPartyWager)} {builder.Environment.EnvironmentName} at {DateTime.Now}");

app.Run();