using System.Net;
using ThirdPartyWager.Extensions;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Workers;

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

            services.AddOptionListByDB<ServerInfo>("Admin_ServerInfoList_Get");
            services.AddOptionListByDB<dynamic>("Service_TpWagerTimer_Get", new { TpId = 1, Type = 1 }, DataBase.Wager);
            services.AddOptionListByDB<dynamic>("Tp_Get", new { TpId = 1 }, DataBase.Main);
            services.AddOptionListByDB<CurrencyOption>("Common_Currency_Get");

            //處理SiteConfig
            services.AddOptionListByDB<ConfigObject>("SiteConfig_Get", new { ConfigGroup = "TP" });
            services.AddSiteConfig();
            services.AddSingleton<ConfigBLL>();

            /*BLL*/
            services.AddSingleton<ThirdPartyBLL>();
            services.AddSingleton<CommonBLL>();

            #region SabaOdds

            // DAL
            services.AddSingleton<SabaOddsService>();
            // BLL
            services.AddSingleton<SabaOddsBLL>();

            services.AddHostedService<SabaOdds>();

            #endregion

            #region LiveStreaming

            // DAL
            services.AddSingleton<LiveStreamingService>();
            // BLL
            services.AddSingleton<LiveStreamingBLL>();

            services.AddHostedService<LiveStreaming>();

            #endregion

            #region LatestHighlightVideo

            // DAL
            services.AddSingleton<LatestHighlightVideoService>();
            // BLL
            services.AddSingleton<LatestHighlightVideoBLL>();

            services.AddHostedService<LatestHighlightVideo>();

            #endregion

            #region TpRefundWorker

            //BLL
            services.AddSingleton<TpRefundBLL>();

            services.AddHostedService<TpRefundWorker>();

            #endregion

            // health check use
            services.AddControllers();

            services.AddTpList();
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
                    await context.Response.WriteAsync($"{env.EnvironmentName}-{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}");
                });

                endpoints.MapControllers();
            });
        }
    }
}
