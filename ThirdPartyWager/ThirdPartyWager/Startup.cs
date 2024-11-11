using System.Net;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ThirdPartyWager.Extensions;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager
{
    public class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddDatabaseConnection(configuration);

            if (!string.IsNullOrEmpty(configuration.GetValue<string>("ProxyHost")))
            {
                var proxy = new HttpClientHandler
                {
                    Proxy = new WebProxy(configuration.GetValue<string>("ProxyHost"), configuration.GetValue<int>("ProxyPort")),
                    UseProxy = true
                };

                services.AddHttpClient("with_proxy")
                .ConfigurePrimaryHttpMessageHandler(() => proxy);
            }
            else
            {
                services.AddHttpClient();
            }

            services
                .AddOpenTelemetry()
                .ConfigureResource(_ => _.AddService(configuration.GetValue<string>("Otel:ServiceName") ?? ""))
                .WithTracing(traceProvider => traceProvider
                .AddAspNetCoreInstrumentation(options => options.Filter =
                     httpContext => !httpContext.Request.Path.HasValue
                     || !httpContext.Request.Path.StartsWithSegments("/swagger")
                     && !httpContext.Request.Path.StartsWithSegments("/health")
                     && !httpContext.Request.Path.StartsWithSegments("/healthcheck"))
                .AddHttpClientInstrumentation()
                .AddConnectorNet()
                .AddOtlpExporter(options => options.Endpoint = configuration.GetValue<Uri>("Otel:Collector") ?? new Uri("")));

            /*Library*/
            services.AddSingleton<HttpHelper>();
            services.AddSingleton<Common>();
            services.AddSingleton<DateTimeHelper>();
            services.AddSingleton<Log2File>();

            /*DAL*/
            services.AddSingleton<ThirdPartyHelper>();
            services.AddSingleton<TpCustInfoService>();
            services.AddSingleton<CommonService>();
            services.AddSingleton<TpUnsettleService>();
            services.AddSingleton<ConfigService>();
            services.AddSingleton<FincanceService>();

            //// 先連線DB減少Worker連線錯誤
            services.AddOptionListByDB<dynamic>("Service_TpWagerTimer_Get", new { TpId = 1, Type = 1 }, DataBase.Wager);
            services.AddOptionListByDB<dynamic>("Tp_Get", new { TpId = 1 }, DataBase.Main);

            services.AddOptionListByDB<CurrencyOption>("Common_Currency_Get");

            //處理SiteConfig
            services.AddOptionListByDB<ServerInfo>("ServerInfoList_Get");
            services.AddOptionListByDB<ConfigObject>("SiteConfig_Get", new { ConfigGroup = "TP" });
            services.AddSiteConfig();
            services.AddSingleton<ConfigBLL>();

            services.AddSingleton<ThirdPartyBLL>();
            services.AddSingleton<TpUnSettledBLL>();
            services.AddSingleton<CommonBLL>();

            services.AddTpList();

            //services.AddLiveStreaming();
            //services.AddLatestHighlightVideo();
            //services.AddSabaOdds();
            services.AddSabaMatches();
            //services.AddTpRefundWorker();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/health", async context =>
                {
                    await context.Response.WriteAsync($"{env.EnvironmentName}-{DateTime.Now:yyyy/MM/dd HH:mm:ss}");
                });
            });
        }
    }
}