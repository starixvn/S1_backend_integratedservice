using System.Net;
using System.Text.Json.Serialization;
using StackExchange.Redis;

using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Extensions.ActionFilters;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.DAL.Interface;
using ThirdPartySeamlessWallet.Service.DAL.ThirdParty;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.Options;

namespace ThirdPartySeamlessWallet
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("BTIPolicy", builder =>
                {
                    builder.AllowAnyOrigin()
                            .AllowAnyHeader()
                            .WithMethods("GET")
                            .SetIsOriginAllowedToAllowWildcardSubdomains();
                });
            });

            services.AddMemoryCache();
            if (!string.IsNullOrEmpty(Configuration.GetValue<string>("ProxyHost")))
            {
                var proxy = new HttpClientHandler
                {
                    Proxy = new WebProxy(Configuration.GetValue<string>("ProxyHost"), Configuration.GetValue<int>("ProxyPort")),
                    UseProxy = true
                };

                services.AddHttpClient("with_proxy")
                .ConfigurePrimaryHttpMessageHandler(() => proxy);
            }
            else
            {
                services.AddHttpClient();
            }
            services.AddHttpClient();
            services.AddMvc().AddXmlSerializerFormatters();
            services.AddDatabaseConnection(Configuration);
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(Configuration.GetValue<string>("RedisConnection")));
            services.AddSingleton<ICacheHelper, RedisCacheHelper>();

            /*Library*/
            services.AddSingleton<Tools>();
            services.AddSingleton<HttpHelper>();
            services.AddSingleton<Log2File>();

            /*DAL*/
            services.AddSingleton<ThirdPartyService>();
            services.AddSingleton<WalletService>();
            services.AddTransient<RiskInspection>();
            services.AddTransient<ICustBetLimitService, CustBetLimitService>();
            services.AddTransient<ISBOService, SBOService>();
            services.AddSingleton<BettingService>();
            services.AddSingleton<TpCustInfoService>();
            services.AddSingleton<SettledService>();
            services.AddSingleton<AuthService>();
            services.AddSingleton<TpUnsettleService>();
            services.AddSingleton<TpRequestFailService>();
            services.AddSingleton<TpUnRewardService>();
            services.AddSingleton<PartitionService>();
            services.AddSingleton<ConfigService>();
            services.AddSingleton<TpRoundGenService>();
            services.AddSingleton<MigrationService>();

            /*BLL*/
            services.AddSingleton<TpCustInfoBLL>();
            services.AddSingleton<ThirdPartyBLL>();
            services.AddSingleton<BettingBLL>();
            services.AddSingleton<SettledBLL>();
            services.AddSingleton<AuthBLL>();
            services.AddSingleton<BonusWalletBLL>();
            services.AddTransient<IKMService, KMService>();
            services.AddTransient<IMGBLL, MGBLL>();
            services.AddSingleton<IHBBLL, HBBLL>();
            services.AddSingleton<IPegasusBLL, PegasusBLL>();
            services.AddSingleton<IIMBLL, IMBLL>();
            services.AddTransient<IAMBBLL, AMBBLL>();
            services.AddTransient<ISBOBLL, SBOBLL>();
            services.AddTransient<INextspinBLL, NextspinBLL>();
            services.AddTransient<IJokerBLL, JokerBLL>();
            services.AddTransient<IKiss918BLL, Kiss918BLL>();
            services.AddTransient<IYeebetBLL, YeebetBLL>();
            services.AddTransient<IBGBLL, BGBLL>();
            services.AddTransient<ITFGBLL, TFGBLL>();
            services.AddTransient<V8BLL>();
            services.AddTransient<AmigoBLL>();
            services.AddTransient<IPNGBLL, PNGBLL>();
            services.AddTransient<IFaChaiBLL, FaChaiBLL>();
            services.AddTransient<IBNGBLL, BNGBLL>();
            services.AddTransient<IGABLL, GABLL>();
            services.AddTransient<INTConnectBLL, NTConnectBLL>();
            services.AddTransient<IWonCasinoBLL, WonCasinoBLL>();
            services.AddTransient<IDragonGamingBLL, DragonGamingBLL>();
            services.AddTransient<IPG2BLL, PG2BLL>();
            services.AddTransient<IEdayBLL, EdayBLL>();
            services.AddSingleton<TpUnSettledBLL>();
            services.AddSingleton<TpBetRecordExtendBLL>();
            services.AddSingleton<TpRequestFailBLL>();
            services.AddSingleton<TpUnRewardBLL>();
            services.AddSingleton<MigrationBLL>();
            services.AddSingleton<KMHeaderFilter>();
            services.AddSingleton<MGHeaderFilter>();
            services.AddSingleton<MGExceptionFilter>();

            services.AddOptionListByDB<SabaSportTypeOption>("Service_SabaSportType_Get");
            //設定SiteConfig
            services.AddOptionListByDB<ServerInfo>("ServerInfoList_Get");
            services.AddOptionListByDB<ConfigObject>("SiteConfig_Get", new { ConfigGroup = "TP" });
            services.AddSiteConfig();
            services.AddSingleton<ConfigBLL>();
            services.AddOptionListByDB<TpBetLimit>("Service_TpBetLimit_Get");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseForwardedHeaders();
            app.UseRouting();
            app.UseCors("BTIPolicy");
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
