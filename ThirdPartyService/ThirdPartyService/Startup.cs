using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Model;
using ThirdPartyService.Extensions;
using ThirdPartyService.Service.BLL;
using ThirdPartyService.Service.DAL;
using ThirdPartyService.Service.Library;

namespace ThirdPartyService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddDatabaseConnection(Configuration);

            /*Library*/
            services.AddSingleton<Tools>();
            services.AddSingleton<Common>();
            //services.AddSingleton<IBCDBLibrary>();
            services.AddSingleton<CacheHelper>();

            /*DAL*/
            services.AddSingleton<ThirdPartyHelper>();
            services.AddSingleton<TpCustInfoService>();
            services.AddSingleton<WalletService>();
            services.AddSingleton<BonusService>();
            services.AddSingleton<TpTransferInfo>();
            services.AddSingleton<TpTransferBonusInfoService>();
            services.AddSingleton<ServerInfoService>();
            services.AddSingleton<AuthService>();
            services.AddSingleton<TpCustBalanceService>();
            services.AddSingleton<TpBetRecordService>();
            services.AddSingleton<ConfigService>();
            services.AddSingleton<BetLimitService>();
            services.AddSingleton<GiftInfoService>();

            /*BLL*/
            services.AddSingleton<ThirdPartyBLL>();
            services.AddSingleton<ServerInfoBLL>();
            services.AddSingleton<TpCustInfoBLL>();
            services.AddSingleton<AuthBLL>();
            services.AddSingleton<GiftInfoBLL>();

            services.AddSingleton<TemporaryLogSentryService>();

            //services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService, WorkerService>();
            services.AddControllers();

            services.AddOptionListByDB<TpGameTypeOption>("Admin_TpGameType_Get");
            services.AddOptionListByDB<GameCasinoOption>("Member_GameCasinoList_Get_V2");
            services.AddOptionListByDB<TpListOption>("Common_TpList_Get");
            services.AddOptionListByDB<CurrencyOption>("Common_Currency_Get");

            //處理SiteConfig
            services.AddOptionListByDB<ServerInfoObject>("ServerInfoList_Get");
            services.AddOptionListByDB<ConfigObject>("SiteConfig_Get", new { ConfigGroup = "TP" });
            services.AddSiteConfig();
            services.AddSingleton<ConfigBLL>();
            services.AddWebProxy(Configuration);

            // 處理SiteLanguage
            services.AddOptionListByDB<SiteLanguageObject>("SiteLanguage_Get", new { LanguageGroup = "TP" });
            services.AddSiteLanguage();
            services.AddSingleton<SiteLanguageBLL>();
            services.AddDbApiLog(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();

            app.UseRouting();
            app.UseRequestLogMiddleware();
            app.UseResponseLogMiddleware();
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