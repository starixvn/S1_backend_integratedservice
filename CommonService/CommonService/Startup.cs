using CommonService.Extensions;
using CommonService.Service.BLL;
using CommonService.Service.DAL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using PaymentService.Service.Library;

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
            services.Configure<Dictionary<string, SendGridConfig>>(Configuration.GetSection("SendGridConfig"));
            services.Configure<IovationServicesConfig>(Configuration.GetSection("IovationServicesConfig"));
            services.Configure<Dictionary<string, FPSServicesConfig>>(Configuration.GetSection("FPSServicesConfig"));
            services.Configure<Dictionary<string, LiveStreamingConfig>>(Configuration.GetSection("LiveStreamingConfig"));
            services.Configure<Dictionary<string, SkyLineSMSServiceTypeConfig>>(Configuration.GetSection("SkyLineSMSServiceConfig"));
            services.Configure<Dictionary<string, YcSmsServiceTypeConfig>>(Configuration.GetSection("YcSMSServiceConfig"));
            services.Configure<Dictionary<string, NxcloudSMSServiceTypeConfig>>(Configuration.GetSection("NxCloudSMSServiceConfig"));
            services.Configure<AWSKeyConfig>(Configuration.GetSection("ailab-sns"));
            services.Configure<Dictionary<string, Dictionary<string, string>>>(Configuration.GetSection("SMSSupportConfig"));
            services.Configure<Dictionary<string, Dictionary<string, string>>>(Configuration.GetSection("Telegram"));
            services.Configure<Dictionary<string, Dictionary<string, string>>>(Configuration.GetSection("LetsTalk"));

            services.AddMemoryCache();
            services.AddDatabaseConnection(Configuration);
            HttpHelper.proxyHost = Configuration.GetValue<string>("ProxyHost");
            HttpHelper.proxyPort = Configuration.GetValue<int>("ProxyPort");

            services.AddSingleton<HttpHelper>();
            services.AddSingleton<AWSSNSHelper>();
            services.AddSingleton<Log2File>();

            services.AddSingleton<CustInfoService>();
            services.AddSingleton<AuthService>();
            services.AddSingleton<IovationService>();
            services.AddSingleton<OTPSMSService>();

            services.AddSingleton<CustInfoBLL>();
            services.AddSingleton<AuthBLL>();
            services.AddSingleton<SendGridBLL>();
            services.AddSingleton<RestPasswordBLL>();
            services.AddSingleton<IovationBLL>();
            services.AddSingleton<OTPSMSBLL>();
            services.AddSingleton<NxCloudSMSBLL>();
            services.AddSingleton<SelLangBLL>();
            services.AddSingleton<FPSBLL>();
            services.AddSingleton<LiveStreamingBLL>();
            services.AddSingleton<OTPEmailBLL>();
            services.AddSingleton<Tools>();

            services.AddControllers();

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
