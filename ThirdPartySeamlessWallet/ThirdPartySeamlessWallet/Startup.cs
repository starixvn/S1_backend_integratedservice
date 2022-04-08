using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Extensions.ActionFilters;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.DAL.Interface;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model.Options;
using ThirdPartySeamlessWallet.Service.Workers;

namespace ThirdPartySeamlessWallet
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
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
            ////if (!string.IsNullOrEmpty(Configuration.GetValue<string>("ProxyHost")))
            ////{
            ////    var proxy = new HttpClientHandler
            ////    {
            ////        Proxy = new WebProxy(Configuration.GetValue<string>("ProxyHost"), Configuration.GetValue<int>("ProxyPort")),
            ////        UseProxy = true
            ////    };

            ////    services.AddHttpClient("with_proxy")
            ////    .ConfigurePrimaryHttpMessageHandler(() => proxy);
            ////    //services.AddHttpClient();
            ////}
            services.AddHttpClient();
            services.AddMvc().AddXmlSerializerFormatters();
            services.AddDatabaseConnection(Configuration);
            /*Library*/
            services.AddSingleton<Tools>();
            services.AddSingleton<HttpHelper>();
            services.AddSingleton<Log2File>();

            /*DAL*/
            services.AddSingleton<ThirdPartyService>();
            services.AddSingleton<WalletService>();
            services.AddTransient<RiskInspection>();
            services.AddTransient<ICustBetLimitService, CustBetLimitService>();
            services.AddSingleton<BettingService>();
            services.AddSingleton<TpCustInfoService>();
            services.AddSingleton<SettledService>();
            services.AddSingleton<AuthService>();
            services.AddSingleton<TpUnsettleService>();
            services.AddSingleton<TpCreditFailService>();
            services.AddSingleton<PartitionService>();
            services.AddSingleton<ConfigService>();

            /*BLL*/
            services.AddSingleton<TpCustInfoBLL>();
            services.AddSingleton<ThirdPartyBLL>();
            services.AddSingleton<BettingBLL>();
            services.AddSingleton<SettledBLL>();
            services.AddSingleton<AuthBLL>();
            services.AddTransient<IKMService, KMService>();
            services.AddTransient<IMGBLL, MGBLL>();
            services.AddSingleton<IHBBLL, HBBLL>();
            services.AddSingleton<TpUnSettledBLL>();
            services.AddSingleton<TpCreditFailBLL>();
            services.AddSingleton<KMHeaderFilter>();
            services.AddSingleton<MGHeaderFilter>();
            services.AddSingleton<MGExceptionFilter>();
            services.AddControllers();

            services.AddOptionListByDB<SabaSportTypeOption>("Service_SabaSportType_Get");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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
