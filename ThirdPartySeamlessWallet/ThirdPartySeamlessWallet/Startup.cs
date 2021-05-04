using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;

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
            services.AddCors(options => options.AddPolicy("BTIPolicy", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyHeader()
                       .WithMethods("GET")
                       .SetIsOriginAllowedToAllowWildcardSubdomains();
            }));

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
            /*Library*/
            services.AddSingleton<Tools>();
            services.AddSingleton<HttpHelper>();
            services.AddSingleton<Log2File>();

            /*DAL*/
            services.AddSingleton<ThirdPartyService>();
            services.AddSingleton<WalletService>();
            services.AddSingleton<BettingService>();
            services.AddSingleton<TpCustInfoService>();
            services.AddSingleton<SettledService>();
            services.AddSingleton<AuthService>();

            /*BLL*/
            services.AddSingleton<TpCustInfoBLL>();
            services.AddSingleton<ThirdPartyBLL>();
            services.AddSingleton<BettingBLL>();
            services.AddSingleton<SettledBLL>();
            services.AddSingleton<AuthBLL>();

            services.AddControllers();
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
