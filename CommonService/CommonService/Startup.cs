using CommonService.Extensions;
using CommonService.Service.BLL;
using CommonService.Service.DAL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using StackExchange.Redis;

namespace CommonService
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();
            services.AddMemoryCache();
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(Configuration.GetValue<string>("RedisConnection")));
            services.AddSingleton<ICacheHelper, RedisCacheHelper>();

            services.AddDatabaseConnection(Configuration);
            HttpHelper.proxyHost = Configuration.GetValue<string>("ProxyHost");
            HttpHelper.proxyPort = Configuration.GetValue<int>("ProxyPort");

            services.AddSingleton<HttpHelper>();
            services.AddSingleton<AWSSNSHelper>();

            services.AddSingleton<CustInfoService>();
            services.AddSingleton<AuthService>();
            services.AddSingleton<IovationService>();
            services.AddSingleton<OTPSMSService>();

            services.AddSingleton<CustInfoBLL>();
            services.AddSingleton<AuthBLL>();
            services.AddSingleton<SendGridBLL>();
            services.AddSingleton<RestPasswordBLL>();
            services.AddSingleton<IovationBLL>();

            services.AddSingleton<SelLangBLL>();
            services.AddSingleton<FPSBLL>();
            services.AddSingleton<LiveStreamingBLL>();
            services.AddSingleton<OTPEmailBLL>();
            services.AddSingleton<Tools>();
            services.AddSingleton<ConfigService>();
            services.AddSingleton<GaBLL>();
            services.AddSingleton<FaceBookPixelBLL>();
            services.AddSingleton<CurrencyService>();
            services.AddControllers();

            //OTP與SMS
            services.AddSingleton<OTPSMSBLL>();
            services.AddSingleton<NxCloudSMSBLL>();
            services.AddSingleton<SkyLineBLL>();
            services.AddSingleton<YCBLL>();
            services.AddSingleton<AWSBLL>();
            services.AddSingleton<SmsCloudBLL>();

            services.AddOptionListByDB<ServerInfo>("ServerInfoList_Get");
            //處理SiteConfig
            services.AddOptionListByDB<Config>("SiteConfig_Get", new { ConfigGroup = "CommonService" });
            services.AddSiteConfig();
            services.AddSingleton<ConfigBLL>();

            // 處理SiteLanguage
            services.AddOptionListByDB<SiteLanguageObject>("SiteLanguage_Get", new { LanguageGroup = "CommonService" });
            services.AddSiteLanguage();
            services.AddSingleton<SiteLanguageBLL>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
           
            app.UseRouting();

            app.UseAuthorization();
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
